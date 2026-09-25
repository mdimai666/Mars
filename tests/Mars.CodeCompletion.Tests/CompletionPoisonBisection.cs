using Mars.CodeCompletion.Contracts.Dto;
using Mars.CodeCompletion.Host.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mars.CodeCompletion.Tests;

public class CompletionPoisonBisection
{
    private static async Task<int> Complete(CodeCompletionWorkspaceManager manager, string docId)
    {
        var completion = new CompletionQueryService(manager, NullLogger<CompletionQueryService>.Instance);
        var result = await completion.GetCompletionsAsync("test.dynamic",
            new CodePositionRequest { DocumentId = docId, Code = "Console.", Offset = 8 }, CancellationToken.None);
        return result.Items.Count;
    }

    [Fact]
    public async Task A_get_compilation_only_poisons()
    {
        using var manager = new CodeCompletionWorkspaceManager([new DynamicGlobalsContextProvider()]);
        Assert.NotEqual(0, await Complete(manager, "a1"));

        var doc = await GetDoc(manager, "a2", "var x = 1;");
        _ = await doc.Project.GetCompilationAsync();

        Assert.NotEqual(0, await Complete(manager, "a3"));
    }

    [Fact]
    public async Task B_syntax_tree_only_poisons()
    {
        using var manager = new CodeCompletionWorkspaceManager([new DynamicGlobalsContextProvider()]);
        Assert.NotEqual(0, await Complete(manager, "b1"));

        var doc = await GetDoc(manager, "b2", "var x = 1;");
        _ = await doc.GetSyntaxTreeAsync();

        Assert.NotEqual(0, await Complete(manager, "b3"));
    }

    [Fact]
    public async Task C_new_document_only_poisons()
    {
        using var manager = new CodeCompletionWorkspaceManager([new DynamicGlobalsContextProvider()]);
        Assert.NotEqual(0, await Complete(manager, "c1"));

        _ = await GetDoc(manager, "c2", "var x = 1;");

        Assert.NotEqual(0, await Complete(manager, "c3"));
    }

    [Fact]
    public async Task D_same_document_second_call()
    {
        using var manager = new CodeCompletionWorkspaceManager([new DynamicGlobalsContextProvider()]);
        Assert.NotEqual(0, await Complete(manager, "d1"));

        // тот же документ, другой текст
        Assert.NotEqual(0, await Complete(manager, "d1"));

        _ = await GetDoc(manager, "d1", "var y = 2; Console.");
        Assert.NotEqual(0, await Complete(manager, "d1"));
    }

    [Fact]
    public async Task E_fresh_document_first_call_after_other_docs_exist()
    {
        using var manager = new CodeCompletionWorkspaceManager([new DynamicGlobalsContextProvider()]);
        _ = await GetDoc(manager, "e1", "var x = 1;");

        // первый completion-запрос в процессе, но документ не первый в проекте
        Assert.NotEqual(0, await Complete(manager, "e2"));
    }

    private static async Task<Microsoft.CodeAnalysis.Document> GetDoc(CodeCompletionWorkspaceManager manager, string docId, string code)
    {
        using var lease = await manager.GetDocumentAsync("test.dynamic", docId, code);
        return lease.Document;
    }
}
