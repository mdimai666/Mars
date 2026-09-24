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
using Microsoft.Extensions.Options;

namespace Mars.CodeCompletion.Host.Services;

/// <summary>
/// Держит один персистентный AdhocWorkspace на контекст (лениво, при первом запросе)
/// и по ОТДЕЛЬНОМУ ПРОЕКТУ на клиентский DocumentId. Submission-проект с несколькими
/// документами Roslyn не поддерживает (completion работает только для первого документа —
/// цепочка submissions строится через project references, а не через соседние документы),
/// поэтому каждый редактор получает свой проект с единственным документом.
/// Текст обновляется на месте, чтобы Roslyn переиспользовал кэш парсинга/компиляции.
///
/// Idle-освобождение: контекст без запросов дольше <c>CodeCompletionOptions.IdleTimeoutMinutes</c>
/// эвиктируется sweep-таймером вместе со всеми документами (утёкшие документы не блокируют —
/// клиент в каждом запросе шлёт полный текст и пересоздаёт документ при следующем attach).
/// Когда контекстов не осталось, сбрасывается и MEF-хост. Эвикция возможна только при
/// in-flight == 0: каждый запрос держит контекст через <see cref="DocumentLease"/>.
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
    private readonly TimeSpan _idleTimeout;
    private readonly TimeSpan _sweepInterval;

    // Защищает: создание/эвикцию контекстов, _hostServices, _sweepTimer, in-flight-счётчики.
    // Держится только на коротких операциях (создание ContextWorkspace — включая ~300
    // MetadataReference.CreateFromFile — происходит под этим локом, один раз на контекст).
    private readonly object _evictionLock = new();

    private Lazy<MefHostServices> _hostServices;
    private Timer? _sweepTimer;
    private int _hostServicesGeneration;

    public CodeCompletionWorkspaceManager(
        IEnumerable<ICodeContextProvider> providers,
        IOptions<CodeCompletionOptions>? options = null,
        ILogger<CodeCompletionWorkspaceManager>? logger = null)
        : this(providers, TimeSpan.FromMinutes(options?.Value.IdleTimeoutMinutes ?? CodeCompletionOptions.DefaultIdleTimeoutMinutes), logger)
    {
    }

    internal CodeCompletionWorkspaceManager(
        IEnumerable<ICodeContextProvider> providers,
        TimeSpan idleTimeout,
        ILogger? logger = null)
    {
        _providers = providers.ToDictionary(p => p.ContextId, StringComparer.Ordinal);
        _logger = logger;
        _idleTimeout = idleTimeout;
        _sweepInterval = TimeSpan.FromMilliseconds(Math.Clamp(idleTimeout.TotalMilliseconds / 4, 50, 60_000));
        _hostServices = new Lazy<MefHostServices>(() => MefHostServices.Create(MefAssemblies));
    }

    public IReadOnlyList<string> ContextIds => _providers.Keys.ToList();

    /// <summary>Живых (созданных) контекстов — для тестов и диагностики.</summary>
    internal int ActiveContextCount
    {
        get
        {
            lock (_evictionLock)
                return _contexts.Count(kvp => kvp.Value.IsValueCreated);
        }
    }

    /// <summary>Инкрементируется при каждом сбросе MEF-хоста — для тестов.</summary>
    internal int HostServicesGeneration => Volatile.Read(ref _hostServicesGeneration);

    /// <summary>
    /// Документ для запроса. Возвращает lease, который ОБЯЗАТЕЛЬНО держать до конца работы
    /// с Document (using) — он защищает контекст от idle-эвикции во время запроса.
    /// </summary>
    public async Task<DocumentLease> GetDocumentAsync(string contextId, string documentId, string code, CancellationToken ct = default)
    {
        ContextWorkspace ctx;
        lock (_evictionLock)
        {
            var lazy = _contexts.GetOrAdd(contextId, id => new Lazy<ContextWorkspace>(() => CreateContext(id)));
            ctx = lazy.Value;
            ctx.Acquire();
            EnsureSweepTimer();
        }

        try
        {
            var document = await ResolveDocumentAsync(ctx, documentId, code, ct);
            return new DocumentLease(document, ctx.Release);
        }
        catch
        {
            ctx.Release();
            throw;
        }
    }

    public void RemoveDocument(string contextId, string documentId)
    {
        lock (_evictionLock)
        {
            if (!_contexts.TryGetValue(contextId, out var lazy) || !lazy.IsValueCreated)
                return;

            var ctx = lazy.Value;
            if (ctx.Documents.TryRemove(documentId, out var ids))
                ctx.Workspace.TryApplyChanges(ctx.Workspace.CurrentSolution.RemoveProject(ids.ProjectId));
        }
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

    private async Task<Document> ResolveDocumentAsync(ContextWorkspace ctx, string documentId, string code, CancellationToken ct)
    {
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

    private void Sweep(object? state)
    {
        List<KeyValuePair<string, ContextWorkspace>>? evicted = null;
        Lazy<MefHostServices>? staleHost = null;

        lock (_evictionLock)
        {
            var now = DateTime.UtcNow;
            foreach (var kvp in _contexts)
            {
                // Lazy без значения — либо кэш исключения CreateContext (неизвестный контекст),
                // либо ещё не инициализирован; создание идёт под этим же локом, поэтому здесь
                // такие записи можно просто вычистить (следующий запрос пересоздаст).
                if (!kvp.Value.IsValueCreated)
                {
                    _contexts.TryRemove(kvp.Key, out _);
                    continue;
                }

                var ctx = kvp.Value.Value;
                if (ctx.InFlight > 0 || now - ctx.LastAccessUtc < _idleTimeout)
                    continue;

                if (_contexts.TryRemove(kvp.Key, out _))
                    (evicted ??= []).Add(new KeyValuePair<string, ContextWorkspace>(kvp.Key, ctx));
            }

            if (_contexts.IsEmpty)
            {
                if (evicted != null)
                {
                    // полная разгрузка: MEF-хост пересоздаётся лениво при следующем запросе
                    staleHost = _hostServices;
                    _hostServices = new Lazy<MefHostServices>(() => MefHostServices.Create(MefAssemblies));
                    Interlocked.Increment(ref _hostServicesGeneration);
                }

                StopSweepTimer();
            }
        }

        if (evicted == null)
            return;

        foreach (var (_, ctx) in evicted)
            ctx.Workspace.Dispose();

        // MEF-хост освобождается сборщиком после потери ссылки (Features-сборки и JIT-код
        // из процесса не выгружаются — десятки МБ, одноразово); Dispose — если вдруг реализует.
        if (staleHost is { IsValueCreated: true } && staleHost.Value is IDisposable disposableHost)
            disposableHost.Dispose();

        _logger?.LogInformation(
            "Code completion: evicted idle context(s) {ContextIds} after {TimeoutMinutes} min of inactivity",
            string.Join(", ", evicted.Select(e => e.Key)),
            _idleTimeout.TotalMinutes);
    }

    private void EnsureSweepTimer()
        => _sweepTimer ??= new Timer(Sweep, null, _sweepInterval, _sweepInterval);

    private void StopSweepTimer()
    {
        _sweepTimer?.Dispose();
        _sweepTimer = null;
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
        lock (_evictionLock)
        {
            StopSweepTimer();
            foreach (var ctx in _contexts.Values)
            {
                if (ctx.IsValueCreated)
                    ctx.Value.Workspace.Dispose();
            }
            _contexts.Clear();
        }
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
        private int _inFlight;
        private long _lastAccessTicks = DateTime.UtcNow.Ticks;

        public AdhocWorkspace Workspace { get; } = workspace;
        public ContextSettings Settings { get; } = settings;
        public ConcurrentDictionary<string, DocumentIds> Documents { get; } = new(StringComparer.Ordinal);
        public Task<CompletionTypeIndex>? TypeIndex;

        public int InFlight => Volatile.Read(ref _inFlight);
        public DateTime LastAccessUtc => new(Interlocked.Read(ref _lastAccessTicks), DateTimeKind.Utc);

        public void Acquire()
        {
            Interlocked.Increment(ref _inFlight);
            Touch();
        }

        public void Release()
        {
            Interlocked.Decrement(ref _inFlight);
            Touch();
        }

        private void Touch() => Interlocked.Exchange(ref _lastAccessTicks, DateTime.UtcNow.Ticks);
    }
}

/// <summary>
/// Lease на время запроса: держит in-flight-счётчик контекста (защита от idle-эвикции)
/// и освобождает его в Dispose. Сервисы обязаны держать lease до конца работы с Document.
/// </summary>
public sealed class DocumentLease(Document document, Action release) : IDisposable
{
    private int _disposed;

    public Document Document { get; } = document;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
            release();
    }
}
