using System.Collections.Concurrent;
using BlazorMonaco;
using BlazorMonaco.Editor;
using BlazorMonaco.Languages;
using Mars.CodeCompletion.Contracts.Dto;
using Mars.WebApiClient.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Range = BlazorMonaco.Range;

namespace Mars.CodeCompletion.Front.Services;

/// <summary>
/// Singleton-реестр подключённых редакторов: провайдеры BlazorMonaco регистрируются на язык глобально
/// (один раз на приложение), а запросы маршрутизируются по modelUri в контекст конкретного редактора.
/// </summary>
public sealed class CodeCompletionRegistry : ICodeCompletionAttacher, IAsyncDisposable
{
    private const string Language = "csharp";

    private readonly IJSRuntime _js;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentDictionary<string, AttachedEditor> _editors = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _initSync = new(1, 1);

    private IJSObjectReference? _module;
    private Task<bool>? _enabledTask;
    private IServiceScope? _scope;
    private ICodeCompletionServiceClient? _client;

    public CodeCompletionRegistry(IJSRuntime js, IServiceScopeFactory scopeFactory)
    {
        _js = js;
        _scopeFactory = scopeFactory;
    }

    private ICodeCompletionServiceClient Client
        => _client ??= (_scope ??= _scopeFactory.CreateScope())
            .ServiceProvider.GetRequiredService<IMarsWebApiClient>().CodeCompletion;

    public Task<bool> IsEnabledAsync() => _enabledTask ??= CheckEnabledAsync();

    private async Task<bool> CheckEnabledAsync()
    {
        try
        {
            var info = await Client.GetInfo();
            Console.WriteLine($"[CodeCompletion] info: enabled={info?.Enabled}, contexts=[{string.Join(", ", info?.Contexts ?? [])}]");
            return info?.Enabled == true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"[CodeCompletion] info check failed: {e.Message}");
            _enabledTask = null;
            return false;
        }
    }

    public async Task<IAsyncDisposable?> AttachAsync(StandaloneCodeEditor editor, string contextId)
    {
        if (!await IsEnabledAsync())
        {
            Console.WriteLine($"[CodeCompletion] attach skipped for '{contextId}': feature disabled on server");
            return null;
        }

        try
        {
            await EnsureInitializedAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine($"[CodeCompletion] init failed: {e}");
            return null;
        }

        if (_module == null)
            return null;

        var model = await editor.GetModel();
        if (string.IsNullOrEmpty(model?.Uri))
        {
            Console.WriteLine($"[CodeCompletion] attach skipped for '{contextId}': editor model not available yet");
            return null;
        }

        var uri = Normalize(model.Uri);
        _editors[uri] = new AttachedEditor(uri, contextId, Guid.NewGuid().ToString("N"));

        await _module.InvokeVoidAsync("watchModel", uri);
        _ = RunDiagnostics(uri);

        Console.WriteLine($"[CodeCompletion] attached '{contextId}' to {uri}");
        return new Attachment(this, uri);
    }

    private async Task DetachAsync(string uri)
    {
        if (!_editors.TryRemove(uri, out var attached))
            return;

        try
        {
            if (_module != null)
            {
                await _module.InvokeVoidAsync("unwatchModel", uri);
                await _module.InvokeVoidAsync("clearMarkers", uri);
            }
        }
        catch (JSDisconnectedException)
        {
        }

        await TryRemoveServerDocumentAsync(attached);
    }

    private async Task EnsureInitializedAsync()
    {
        if (_module != null)
            return;

        await _initSync.WaitAsync();
        try
        {
            if (_module != null)
                return;

            var module = await _js.InvokeAsync<IJSObjectReference>("import", CodeCompletionAssets.JsModuleUrl);
            await module.InvokeVoidAsync("init", DotNetObjectReference.Create(this));

            await BlazorMonaco.Languages.Global.RegisterCompletionItemProvider(
                _js,
                new LanguageSelector(Language),
                new CompletionItemProvider(null, ProvideCompletionItems) { TriggerCharacters = ["."] });

            await BlazorMonaco.Languages.Global.RegisterHoverProviderAsync(
                _js,
                new LanguageSelector(Language),
                ProvideHover);

            _module = module;
        }
        finally
        {
            _initSync.Release();
        }
    }

    private async Task<CompletionList> ProvideCompletionItems(string modelUri, Position position, CompletionContext context)
    {
        var empty = new CompletionList { Suggestions = [], Incomplete = false };
        if (!_editors.TryGetValue(Normalize(modelUri), out var attached))
            return empty;

        try
        {
            var snapshot = await GetSnapshotAsync(attached, position);
            if (snapshot == null)
                return empty;

            var response = await Client.GetCompletions(attached.ContextId, ToRequest(attached, snapshot));

            var suggestions = response.Items.Select(i => new CompletionItem
            {
                LabelAsString = i.Label,
                Kind = ParseKind(i.Kind),
                InsertText = i.InsertText ?? i.Label,
                FilterText = i.FilterText,
                SortText = i.SortText,
                Detail = i.Detail,
                DocumentationAsString = i.Documentation,
            }).ToList();

            return new CompletionList { Suggestions = suggestions, Incomplete = response.Incomplete };
        }
        catch
        {
            return empty;
        }
    }

    private async Task<Hover?> ProvideHover(string modelUri, Position position, HoverContext context)
    {
        if (!_editors.TryGetValue(Normalize(modelUri), out var attached))
            return null;

        try
        {
            var snapshot = await GetSnapshotAsync(attached, position);
            if (snapshot == null)
                return null;

            var hover = await Client.GetHover(attached.ContextId, ToRequest(attached, snapshot));
            if (hover == null)
                return null;

            return new Hover
            {
                Contents = [new MarkdownString { Value = hover.Content }],
                Range = OffsetsToRange(snapshot.Code, hover.OffsetFrom, hover.OffsetTo),
            };
        }
        catch
        {
            return null;
        }
    }

    [JSInvokable]
    public async Task<SignatureHelpResponseDto?> ProvideSignatureHelp(string modelUri, Position position)
    {
        if (!_editors.TryGetValue(Normalize(modelUri), out var attached))
            return null;

        try
        {
            var snapshot = await GetSnapshotAsync(attached, position);
            if (snapshot == null)
                return null;

            return await Client.GetSignatureHelp(attached.ContextId, ToRequest(attached, snapshot));
        }
        catch
        {
            return null;
        }
    }

    [JSInvokable]
    public async Task RunDiagnostics(string modelUri)
    {
        var uri = Normalize(modelUri);
        if (!_editors.TryGetValue(uri, out var attached) || _module == null)
            return;

        try
        {
            var snapshot = await GetSnapshotAsync(attached, null);
            if (snapshot == null)
                return;

            var diagnostics = await Client.GetDiagnostics(attached.ContextId, ToRequest(attached, snapshot));
            await _module.InvokeVoidAsync("setMarkers", uri, diagnostics);
        }
        catch (JSDisconnectedException)
        {
        }
        catch
        {
            // диагностика — фоновая проверка, ошибки не должны ломать редактирование
        }
    }

    [JSInvokable]
    public async Task RemoveDocument(string modelUri)
    {
        if (_editors.TryRemove(Normalize(modelUri), out var attached))
            await TryRemoveServerDocumentAsync(attached);
    }

    private async Task TryRemoveServerDocumentAsync(AttachedEditor attached)
    {
        try
        {
            await Client.RemoveDocument(attached.ContextId, attached.DocumentId);
        }
        catch
        {
            // серверный документ рано или поздно пересоздаётся; утечка проекта некритична
        }
    }

    private async Task<ModelSnapshot?> GetSnapshotAsync(AttachedEditor attached, Position? position)
    {
        if (_module == null)
            return null;
        return await _module.InvokeAsync<ModelSnapshot?>("getModelSnapshot", attached.Uri, position);
    }

    private static CodePositionRequest ToRequest(AttachedEditor attached, ModelSnapshot snapshot)
        => new()
        {
            DocumentId = attached.DocumentId,
            Code = snapshot.Code,
            Offset = snapshot.Offset,
        };

    private static string Normalize(string uri) => Uri.UnescapeDataString(uri);

    private static CompletionItemKind ParseKind(string kind)
        => Enum.TryParse<CompletionItemKind>(kind, ignoreCase: true, out var parsed)
            ? parsed
            : CompletionItemKind.Text;

    private static Range OffsetsToRange(string code, int from, int to)
    {
        var (startLine, startColumn) = ToLineColumn(code, from);
        var (endLine, endColumn) = ToLineColumn(code, to);
        return new Range
        {
            StartLineNumber = startLine,
            StartColumn = startColumn,
            EndLineNumber = endLine,
            EndColumn = endColumn,
        };
    }

    private static (int Line, int Column) ToLineColumn(string code, int offset)
    {
        offset = Math.Clamp(offset, 0, code.Length);
        var line = 1;
        var lineStart = 0;
        for (var i = 0; i < offset; i++)
        {
            if (code[i] == '\n')
            {
                line++;
                lineStart = i + 1;
            }
        }

        return (line, offset - lineStart + 1);
    }

    public async ValueTask DisposeAsync()
    {
        _editors.Clear();
        if (_module != null)
            await _module.DisposeAsync();
        _scope?.Dispose();
        _initSync.Dispose();
    }

    private sealed record AttachedEditor(string Uri, string ContextId, string DocumentId);

    private sealed class Attachment(CodeCompletionRegistry registry, string uri) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => new(registry.DetachAsync(uri));
    }

    private sealed record ModelSnapshot(string Code, int Offset);
}
