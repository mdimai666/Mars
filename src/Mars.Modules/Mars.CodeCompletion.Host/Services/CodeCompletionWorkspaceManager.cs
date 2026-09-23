using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using Mars.CodeCompletion.Host.Abstractions;
using Mars.Core.Exceptions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Mars.CodeCompletion.Host.Services;

/// <summary>
/// Держит по одному персистентному AdhocWorkspace на контекст (лениво, при первом запросе)
/// и по документу на клиентский DocumentId; текст обновляется на месте, чтобы Roslyn
/// переиспользовал кэш парсинга/компиляции между запросами.
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
    private readonly ConcurrentDictionary<string, Lazy<ContextWorkspace>> _contexts = new(StringComparer.Ordinal);
    private readonly Lazy<MefHostServices> _hostServices = new(() => MefHostServices.Create(MefAssemblies));

    public CodeCompletionWorkspaceManager(IEnumerable<ICodeContextProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.ContextId, StringComparer.Ordinal);
    }

    public IReadOnlyList<string> ContextIds => _providers.Keys.ToList();

    public async Task<Document> GetDocumentAsync(string contextId, string documentId, string code, CancellationToken ct = default)
    {
        var ctx = _contexts.GetOrAdd(contextId, id => new Lazy<ContextWorkspace>(() => CreateContext(id))).Value;

        if (!ctx.Documents.TryGetValue(documentId, out var docId))
        {
            lock (ctx.Documents)
            {
                if (!ctx.Documents.TryGetValue(documentId, out docId))
                {
                    var info = DocumentInfo.Create(
                        DocumentId.CreateNewId(ctx.ProjectId),
                        $"Doc_{documentId}",
                        loader: TextLoader.From(TextAndVersion.Create(SourceText.From("", Encoding.UTF8), VersionStamp.Create())));
                    ctx.Workspace.AddDocument(info);
                    docId = info.Id;
                    ctx.Documents[documentId] = docId;
                }
            }
        }

        var document = ctx.Workspace.CurrentSolution.GetDocument(docId)
            ?? throw new UserActionException($"Completion document '{documentId}' is gone");

        var text = SourceText.From(code, Encoding.UTF8);
        var currentText = await document.GetTextAsync(ct);
        if (!currentText.ContentEquals(text))
        {
            ctx.Workspace.TryApplyChanges(ctx.Workspace.CurrentSolution.WithDocumentText(docId, text));
            document = ctx.Workspace.CurrentSolution.GetDocument(docId)!;
        }

        return document;
    }

    private ContextWorkspace CreateContext(string contextId)
    {
        if (!_providers.TryGetValue(contextId, out var provider))
            throw new NotFoundException($"Code completion context '{contextId}' is not registered");

        var parseOptions = new CSharpParseOptions(
            LanguageVersion.Latest,
            DocumentationMode.Parse,
            provider.IsScript ? SourceCodeKind.Script : SourceCodeKind.Regular);

        var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, usings: provider.Imports);

        var references = TrustedPlatformReferences()
            .Concat(provider.GetMetadataReferences())
            .GroupBy(r => r.Display, StringComparer.Ordinal)
            .Select(g => g.First())
            .ToList();

        var workspace = new AdhocWorkspace(_hostServices.Value);
        var projectInfo = ProjectInfo.Create(
            ProjectId.CreateNewId(),
            VersionStamp.Create(),
            $"MarsCodeCompletion.{contextId}",
            $"MarsCodeCompletion_{contextId}",
            LanguageNames.CSharp,
            compilationOptions: compilationOptions,
            parseOptions: parseOptions,
            metadataReferences: references,
            hostObjectType: provider.HostObjectType);

        var project = workspace.AddProject(projectInfo);
        return new ContextWorkspace(workspace, project.Id);
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

    private sealed class ContextWorkspace(AdhocWorkspace workspace, ProjectId projectId)
    {
        public AdhocWorkspace Workspace { get; } = workspace;
        public ProjectId ProjectId { get; } = projectId;
        public ConcurrentDictionary<string, DocumentId> Documents { get; } = new(StringComparer.Ordinal);
    }
}
