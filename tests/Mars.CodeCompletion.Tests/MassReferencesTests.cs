using Mars.CodeCompletion.Contracts.Dto;
using Mars.CodeCompletion.Host.Abstractions;
using Mars.CodeCompletion.Host.Services;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mars.CodeCompletion.Tests;

public class MassReferencesContextProvider : ICodeContextProvider
{
    public string ContextId => "test.mass";

    public Type? HostObjectType => typeof(TestGlobals);

    public IReadOnlyList<string> Imports { get; } = ["System", "System.Collections.Generic", "System.Linq"];

    public IReadOnlyCollection<MetadataReference> GetMetadataReferences()
    {
        var loaded = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location));

        var tpa = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(p =>
            {
                try { return MetadataReference.CreateFromFile(p); }
                catch (BadImageFormatException) { return null; }
            })
            .Where(r => r != null)
            .Select(r => r!);

        return loaded.Concat(tpa).ToList();
    }
}

public class DynamicGlobals
{
    public dynamic msg = default!;
    public string NodeId = "";
}

public class DynamicGlobalsContextProvider : ICodeContextProvider
{
    public string ContextId => "test.dynamic";

    public Type? HostObjectType => typeof(DynamicGlobals);

    public IReadOnlyList<string> Imports { get; } = ["System", "System.Linq"];

    public IReadOnlyCollection<MetadataReference> GetMetadataReferences() => [];
}

public class MassReferencesTests
{
    [Fact]
    public async Task Mass_references_context_compiles()
    {
        using var manager = new CodeCompletionWorkspaceManager([new MassReferencesContextProvider()]);
        var diagnostics = new DiagnosticsQueryService(manager, NullLogger<DiagnosticsQueryService>.Instance);

        var result = await diagnostics.GetDiagnosticsAsync("test.mass",
            new CodePositionRequest { DocumentId = "d1", Code = "var x = 1;", Offset = 10 }, CancellationToken.None);

        Assert.DoesNotContain(result, d => d.Severity == 8);
    }

    [Fact]
    public async Task Mass_references_completion_works()
    {
        using var manager = new CodeCompletionWorkspaceManager([new MassReferencesContextProvider()]);
        var completion = new CompletionQueryService(manager, NullLogger<CompletionQueryService>.Instance);

        var result = await completion.GetCompletionsAsync("test.mass",
            new CodePositionRequest { DocumentId = "d2", Code = "Console.", Offset = 8 }, CancellationToken.None);

        Assert.Contains(result.Items, i => i.Label == "WriteLine");
    }

    [Fact]
    public async Task Dynamic_global_field_compiles()
    {
        using var manager = new CodeCompletionWorkspaceManager([new DynamicGlobalsContextProvider()]);
        var diagnostics = new DiagnosticsQueryService(manager, NullLogger<DiagnosticsQueryService>.Instance);

        var result = await diagnostics.GetDiagnosticsAsync("test.dynamic",
            new CodePositionRequest { DocumentId = "d1", Code = "var x = msg;", Offset = 12 }, CancellationToken.None);

        Assert.DoesNotContain(result, d => d.Severity == 8);
    }

    [Fact]
    public async Task Dynamic_globals_completion_works()
    {
        using var manager = new CodeCompletionWorkspaceManager([new DynamicGlobalsContextProvider()]);
        var completion = new CompletionQueryService(manager, NullLogger<CompletionQueryService>.Instance);

        var result = await completion.GetCompletionsAsync("test.dynamic",
            new CodePositionRequest { DocumentId = "d2", Code = "Console.", Offset = 8 }, CancellationToken.None);

        Assert.Contains(result.Items, i => i.Label == "WriteLine");
    }

    [Fact]
    public async Task Empty_prefix_completion_is_capped_and_incomplete()
    {
        using var manager = new CodeCompletionWorkspaceManager([new MassReferencesContextProvider()]);
        var completion = new CompletionQueryService(manager, NullLogger<CompletionQueryService>.Instance);

        var result = await completion.GetCompletionsAsync("test.mass",
            new CodePositionRequest { DocumentId = "d6", Code = "var x = 1; ", Offset = 11 }, CancellationToken.None);

        Assert.Equal(200, result.Items.Count);
        Assert.True(result.Incomplete);
    }

    [Fact]
    public async Task Prefixed_completion_is_filtered_and_complete()
    {
        using var manager = new CodeCompletionWorkspaceManager([new MassReferencesContextProvider()]);
        var completion = new CompletionQueryService(manager, NullLogger<CompletionQueryService>.Instance);

        var result = await completion.GetCompletionsAsync("test.mass",
            new CodePositionRequest { DocumentId = "d7", Code = "Consol", Offset = 6 }, CancellationToken.None);

        Assert.Contains(result.Items, i => i.Label == "Console");
        Assert.True(result.Items.Count < 200);
        Assert.False(result.Incomplete);
    }

    [Fact]
    public async Task Completion_still_works_after_diagnostics()
    {
        using var manager = new CodeCompletionWorkspaceManager([new DynamicGlobalsContextProvider()]);
        var completion = new CompletionQueryService(manager, NullLogger<CompletionQueryService>.Instance);
        var diagnostics = new DiagnosticsQueryService(manager, NullLogger<DiagnosticsQueryService>.Instance);

        var first = await completion.GetCompletionsAsync("test.dynamic",
            new CodePositionRequest { DocumentId = "d3", Code = "Console.", Offset = 8 }, CancellationToken.None);
        Assert.Contains(first.Items, i => i.Label == "WriteLine");

        _ = await diagnostics.GetDiagnosticsAsync("test.dynamic",
            new CodePositionRequest { DocumentId = "d4", Code = "var x = 1;", Offset = 10 }, CancellationToken.None);

        var second = await completion.GetCompletionsAsync("test.dynamic",
            new CodePositionRequest { DocumentId = "d5", Code = "Console.", Offset = 8 }, CancellationToken.None);
        Assert.Contains(second.Items, i => i.Label == "WriteLine");
    }
}
