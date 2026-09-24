using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using Mars.CodeCompletion.Host.Abstractions;
using Mars.Core.Exceptions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;

namespace Mars.CodeCompletion.Host.Services;

/// <summary>
/// Держит один персистентный AdhocWorkspace на контекст (лениво, при первом запросе)
/// и по ОТДЕЛЬНОМУ ПРОЕКТУ на клиентский DocumentId. Submission-проект с несколькими
/// документами Roslyn не поддерживает (completion работает только для первого документа —
/// цепочка submissions строится через project references, а не через соседние документы),
/// поэтому каждый редактор получает свой проект с единственным документом.
/// Текст обновляется на месте, чтобы Roslyn переиспользовал кэш парсинга/компиляции.
/// </summary>
public sealed class CodeCompletionWorkspaceManager : IDisposable
{
    // Features-сборки не имеют публичных типов-якорей; Assembly.Load безопасен —
    // все четыре сборки гарантированно в выходе через PackageReference.
    private static readonly Assembly[] MefAssemblies =
    [
        Assembly.Load("Microsoft.CodeAnalysis.Workspaces"),
        Assembly.Load("Microsoft.CodeAnalysis.CSharp.Workspaces"),
        Assembly.Load("Microsoft.CodeAnalysis.Features"),
        Assembly.Load("Microsoft.CodeAnalysis.CSharp.Features"),
    ];

    private readonly IReadOnlyDictionary<string, ICodeContextProvider> _providers;
    private readonly ILogger? _logger;
    private readonly ConcurrentDictionary<string, Lazy<ContextWorkspace>> _contexts = new(StringComparer.Ordinal);
    private readonly Lazy<MefHostServices> _hostServices = new(() => MefHostServices.Create(MefAssemblies));

    public CodeCompletionWorkspaceManager(
        IEnumerable<ICodeContextProvider> providers,
        ILogger<CodeCompletionWorkspaceManager>? logger = null)
    {
        _providers = providers.ToDictionary(p => p.ContextId, StringComparer.Ordinal);
        _logger = logger;
    }

    public IReadOnlyList<string> ContextIds => _providers.Keys.ToList();

    public async Task<Document> GetDocumentAsync(string contextId, string documentId, string code, CancellationToken ct = default)
    {
        var ctx = _contexts.GetOrAdd(contextId, id => new Lazy<ContextWorkspace>(() => CreateContext(id))).Value;

        if (ctx.Documents.TryGetValue(documentId, out var ids))
        {
            var document = ctx.Workspace.CurrentSolution.GetDocument(ids.DocumentId)
                ?? throw new UserActionException($"Completion document '{documentId}' is gone");

            var text = SourceText.From(code, Encoding.UTF8);
            var currentText = await document.GetTextAsync(ct);
            if (!currentText.ContentEquals(text))
            {
                ctx.Workspace.TryApplyChanges(ctx.Workspace.CurrentSolution.WithDocumentText(ids.DocumentId, text));
                document = ctx.Workspace.CurrentSolution.GetDocument(ids.DocumentId)!;
            }

            return document;
        }

        lock (ctx.Documents)
        {
            if (ctx.Documents.TryGetValue(documentId, out ids))
                return ctx.Workspace.CurrentSolution.GetDocument(ids.DocumentId)!;

            var text = SourceText.From(code, Encoding.UTF8);
            var projectId = ProjectId.CreateNewId();
            var documentInfo = DocumentInfo.Create(
                DocumentId.CreateNewId(projectId),
                $"Doc_{documentId}",
                sourceCodeKind: ctx.Settings.ParseOptions.Kind,
                loader: TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create())));

            var projectInfo = ctx.Settings.CreateProjectInfo(projectId).WithDocuments([documentInfo]);
            ctx.Workspace.AddProject(projectInfo);

            ids = new DocumentIds(projectId, documentInfo.Id);
            ctx.Documents[documentId] = ids;
        }

        return ctx.Workspace.CurrentSolution.GetDocument(ids.DocumentId)
            ?? throw new UserActionException($"Completion document '{documentId}' is gone");
    }

    public void RemoveDocument(string contextId, string documentId)
    {
        if (!_contexts.TryGetValue(contextId, out var lazy) || !lazy.IsValueCreated)
            return;

        var ctx = lazy.Value;
        if (ctx.Documents.TryRemove(documentId, out var ids))
            ctx.Workspace.TryApplyChanges(ctx.Workspace.CurrentSolution.RemoveProject(ids.ProjectId));
    }

    /// <summary>Индекс «имя типа → namespaces» по ссылкам контекста; строится один раз (лениво).</summary>
    public Task<CompletionTypeIndex> GetTypeIndexAsync(string contextId, Document document, CancellationToken ct)
    {
        if (!_contexts.TryGetValue(contextId, out var lazy) || !lazy.IsValueCreated)
            throw new NotFoundException($"Code completion context '{contextId}' is not registered");

        var ctx = lazy.Value;
        lock (ctx)
        {
            return ctx.TypeIndex ??= CompletionTypeIndex.CreateAsync(document.Project, _logger, ct);
        }
    }

    private ContextWorkspace CreateContext(string contextId)
    {
        if (!_providers.TryGetValue(contextId, out var provider))
            throw new NotFoundException($"Code completion context '{contextId}' is not registered");

        var references = TrustedPlatformReferences()
            .Concat(provider.GetMetadataReferences())
            .GroupBy(r => r.Display, StringComparer.Ordinal)
            .Select(g => g.First())
            .ToList();

        var settings = new ContextSettings(
            contextId,
            new CSharpParseOptions(
                LanguageVersion.Latest,
                DocumentationMode.Parse,
                provider.IsScript ? SourceCodeKind.Script : SourceCodeKind.Regular),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, usings: provider.Imports),
            references,
            provider.IsScript,
            provider.HostObjectType);

        return new ContextWorkspace(new AdhocWorkspace(_hostServices.Value), settings);
    }

    private static IEnumerable<MetadataReference> TrustedPlatformReferences()
    {
        var tpa = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (string.IsNullOrEmpty(tpa))
            yield break;

        foreach (var path in tpa.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            MetadataReference reference;
            try
            {
                reference = MetadataReference.CreateFromFile(path);
            }
            catch (BadImageFormatException)
            {
                continue;
            }
            yield return reference;
        }
    }

    public void Dispose()
    {
        foreach (var ctx in _contexts.Values)
        {
            if (ctx.IsValueCreated)
                ctx.Value.Workspace.Dispose();
        }
        _contexts.Clear();
    }

    private sealed record DocumentIds(ProjectId ProjectId, DocumentId DocumentId);

    private sealed record ContextSettings(
        string ContextId,
        CSharpParseOptions ParseOptions,
        CSharpCompilationOptions CompilationOptions,
        IReadOnlyList<MetadataReference> References,
        bool IsSubmission,
        Type? HostObjectType)
    {
        public ProjectInfo CreateProjectInfo(ProjectId projectId)
            => ProjectInfo.Create(
                projectId,
                VersionStamp.Create(),
                $"MarsCodeCompletion.{ContextId}.{projectId.Id}",
                $"MarsCodeCompletion_{ContextId}_{projectId.Id}",
                LanguageNames.CSharp,
                compilationOptions: CompilationOptions,
                parseOptions: ParseOptions,
                metadataReferences: References,
                // globals (HostObjectType) применяются Roslyn только к submission-проектам
                isSubmission: IsSubmission,
                hostObjectType: HostObjectType);
    }

    private sealed class ContextWorkspace(AdhocWorkspace workspace, ContextSettings settings)
    {
        public AdhocWorkspace Workspace { get; } = workspace;
        public ContextSettings Settings { get; } = settings;
        public ConcurrentDictionary<string, DocumentIds> Documents { get; } = new(StringComparer.Ordinal);
        public Task<CompletionTypeIndex>? TypeIndex;
    }
}
