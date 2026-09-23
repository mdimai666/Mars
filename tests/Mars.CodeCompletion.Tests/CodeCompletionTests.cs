using Mars.CodeCompletion.Contracts.Dto;
using Mars.CodeCompletion.Host.Services;
using Mars.Core.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mars.CodeCompletion.Tests;

public class CodeCompletionTests : IDisposable
{
    private readonly CodeCompletionWorkspaceManager _manager = new([new TestCodeContextProvider()]);

    private CompletionQueryService Completion => new(_manager, NullLogger<CompletionQueryService>.Instance);
    private HoverQueryService Hover => new(_manager, NullLogger<HoverQueryService>.Instance);
    private SignatureHelpQueryService SignatureHelp => new(_manager);
    private DiagnosticsQueryService Diagnostics => new(_manager);

    public void Dispose() => _manager.Dispose();

    private static CodePositionRequest Request(string code, string marker = "|")
    {
        var offset = code.IndexOf(marker, StringComparison.Ordinal);
        return new CodePositionRequest
        {
            DocumentId = Guid.NewGuid().ToString("N"),
            Code = offset >= 0 ? code.Replace(marker, "") : code,
            Offset = offset >= 0 ? offset : code.Length,
        };
    }

    [Fact]
    public async Task Script_top_level_return_compiles_without_errors()
    {
        var diagnostics = await Diagnostics.GetDiagnosticsAsync(
            TestCodeContextProvider.Id, Request("return msg.Payload;"), CancellationToken.None);

        Assert.DoesNotContain(diagnostics, d => d.Severity == 8);
    }

    [Fact]
    public async Task Script_sees_global_object_members_in_completion()
    {
        var completions = await Completion.GetCompletionsAsync(
            TestCodeContextProvider.Id, Request("msg.|"), CancellationToken.None);

        Assert.Contains(completions.Items, i => i.Label == "Payload");
    }

    [Fact]
    public async Task Script_completion_includes_globals_methods_and_locals()
    {
        var completions = await Completion.GetCompletionsAsync(
            TestCodeContextProvider.Id, Request("var localVar = 1; |"), CancellationToken.None);

        var labels = completions.Items.Select(i => i.Label).ToList();
        Assert.Contains("Send", labels);
        Assert.Contains("Add", labels);
        Assert.Contains("localVar", labels);
    }

    [Fact]
    public async Task Imports_are_visible_in_completion()
    {
        var completions = await Completion.GetCompletionsAsync(
            TestCodeContextProvider.Id, Request("Enumerable.|"), CancellationToken.None);

        // System.Linq импортирован контекстом — статические члены доступны без using
        Assert.Contains(completions.Items, i => i.Label == "Empty");
    }

    [Fact]
    public async Task Diagnostics_report_error_with_offsets()
    {
        var code = "var x = undefinedSymbol;";
        var diagnostics = await Diagnostics.GetDiagnosticsAsync(
            TestCodeContextProvider.Id, Request(code), CancellationToken.None);

        var error = Assert.Single(diagnostics, d => d.Severity == 8);
        Assert.Equal("CS0103", error.Id);
        Assert.Equal(code.IndexOf("undefinedSymbol", StringComparison.Ordinal), error.OffsetFrom);
        Assert.Equal(code.IndexOf("undefinedSymbol", StringComparison.Ordinal) + "undefinedSymbol".Length, error.OffsetTo);
    }

    [Fact]
    public async Task Hover_returns_symbol_info()
    {
        var hover = await Hover.GetHoverAsync(
            TestCodeContextProvider.Id, Request("msg.Pay|load"), CancellationToken.None);

        Assert.NotNull(hover);
        Assert.Contains("Payload", hover.Content);
    }

    [Fact]
    public async Task SignatureHelp_returns_overload_and_active_parameter()
    {
        var help = await SignatureHelp.GetSignatureHelpAsync(
            TestCodeContextProvider.Id, Request("Add(1, |)"), CancellationToken.None);

        Assert.NotNull(help);
        var signature = Assert.Single(help.Signatures);
        Assert.Contains("Add", signature.Label);
        Assert.Equal(2, signature.Parameters.Count);
        Assert.Equal(1, help.ActiveParameter);
    }

    [Fact]
    public async Task Unknown_context_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Completion.GetCompletionsAsync(
            "unknown.context", Request("msg.|"), CancellationToken.None));
    }
}
