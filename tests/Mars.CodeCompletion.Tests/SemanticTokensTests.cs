using Mars.CodeCompletion.Contracts.Dto;
using Mars.CodeCompletion.Host.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mars.CodeCompletion.Tests;

public class SemanticTokensTests
{
    private static readonly int Namespace = Array.IndexOf(SemanticTokensQueryService.TokenTypes, "namespace");
    private static readonly int Class = Array.IndexOf(SemanticTokensQueryService.TokenTypes, "class");
    private static readonly int Method = Array.IndexOf(SemanticTokensQueryService.TokenTypes, "method");
    private static readonly int String = Array.IndexOf(SemanticTokensQueryService.TokenTypes, "string");
    private static readonly int Keyword = Array.IndexOf(SemanticTokensQueryService.TokenTypes, "keyword");

    private static async Task<IReadOnlyList<int>> TokensFor(string code)
    {
        using var manager = new CodeCompletionWorkspaceManager([new TestCodeContextProvider()]);
        var service = new SemanticTokensQueryService(manager, NullLogger<SemanticTokensQueryService>.Instance);
        return await service.GetSemanticTokensDataAsync(TestCodeContextProvider.Id,
            new CodePositionRequest { DocumentId = Guid.NewGuid().ToString("N"), Code = code, Offset = code.Length },
            CancellationToken.None);
    }

    private static IEnumerable<int> TokenTypes(IReadOnlyList<int> data)
    {
        for (var i = 3; i < data.Count; i += 5)
            yield return data[i];
    }

    [Fact]
    public async Task Encodes_five_ints_per_token()
    {
        var data = await TokensFor("var s = \"abc\";");

        Assert.NotEmpty(data);
        Assert.Equal(0, data.Count % 5);
    }

    [Fact]
    public async Task Classifies_keyword_and_string()
    {
        var data = await TokensFor("var s = \"abc\";");
        var types = TokenTypes(data).ToList();

        Assert.Contains(Keyword, types);
        Assert.Contains(String, types);
    }

    [Fact]
    public async Task Classifies_namespace_class_and_method()
    {
        var data = await TokensFor("System.Console.WriteLine(\"x\");");
        var types = TokenTypes(data).ToList();

        Assert.Contains(Namespace, types);
        Assert.Contains(Class, types);
        Assert.Contains(Method, types);
    }

    [Fact]
    public async Task Delta_encoding_starts_at_first_line()
    {
        var data = await TokensFor("\n\nvar s = 1;");

        // первый токен на строке 2 (0-based) — deltaLine от начала документа
        Assert.Equal(2, data[0]);
        // все последующие — одна строка, deltaLine = 0
        for (var i = 5; i < data.Count; i += 5)
            Assert.Equal(0, data[i]);
    }
}
