using FluentAssertions;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Mars.Datasource.Providers.Rest;

namespace Mars.Datasource.Tests.RestProviders;

/// <summary>
/// Документ запросов `.http` (синтаксис VS Code REST Client): блоки, имена, переменные, заголовки и тела.
/// </summary>
public class HttpDocumentParserTests
{
    [Fact]
    public void Parse_SingleRequest_ReadsMethodUrlHeadersAndBody()
    {
        var document = HttpDocumentParser.Parse("""
            POST /wp-json/wp/v2/posts
            Content-Type: application/json
            Accept: application/json

            {"title":"Hello"}
            """);

        document.Requests.Should().ContainSingle();

        var request = document.Requests[0];
        request.Method.Should().Be("POST");
        request.Url.Should().Be("/wp-json/wp/v2/posts");
        request.Headers.Should().Equal(
            new HttpDocumentHeader("Content-Type", "application/json"),
            new HttpDocumentHeader("Accept", "application/json"));
        request.Body.Should().Be("""{"title":"Hello"}""");
        request.IsWrite.Should().BeTrue();
    }

    [Fact]
    public void Parse_SeparatorSplitsRequests()
    {
        var document = HttpDocumentParser.Parse("""
            GET /a

            ###

            GET /b
            """);

        document.Requests.Select(request => request.Url).Should().Equal("/a", "/b");
    }

    [Fact]
    public void Parse_NameDirectiveSetsRequestName()
    {
        var document = HttpDocumentParser.Parse("""
            ###
            # @name listPosts
            GET /wp/v2/posts
            """);

        document.Requests[0].Name.Should().Be("listPosts");
        document.ByName("LISTPOSTS").Should().NotBeNull();
    }

    [Fact]
    public void Parse_SeparatorLabelIsNameWhenNoDirective()
    {
        var document = HttpDocumentParser.Parse("""
            ### Список постов
            GET /wp/v2/posts
            """);

        document.Requests[0].Name.Should().Be("Список постов");
    }

    [Fact]
    public void Parse_CommentsAndProtocolVersionAreIgnored()
    {
        var document = HttpDocumentParser.Parse("""
            # комментарий к документу
            // и такой комментарий

            ###
            # @name me
            GET https://example.org/wp-json/wp/v2/users/me HTTP/1.1
            """);

        var request = document.Requests.Should().ContainSingle().Subject;
        request.Url.Should().Be("https://example.org/wp-json/wp/v2/users/me");
    }

    [Fact]
    public void Parse_VariablesBeforeFirstRequestAreDocumentLevel()
    {
        var document = HttpDocumentParser.Parse("""
            @host = https://example.org
            @perPage=10

            ###
            GET {{host}}/wp/v2/posts
            """);

        document.Variables.Should().ContainKeys("host", "perPage");
        document.Variables["host"].Should().Be("https://example.org");
        document.Variables["perPage"].Should().Be("10");
    }

    [Fact]
    public void Parse_VariablesInsideBlockAreRequestLevel()
    {
        var document = HttpDocumentParser.Parse("""
            ###
            @id = 42
            GET /wp/v2/posts/{{id}}
            """);

        document.Variables.Should().BeEmpty();
        document.Requests[0].Variables.Should().ContainKey("id");
    }

    [Fact]
    public void Parse_UrlWithoutMethodDefaultsToGet()
    {
        var document = HttpDocumentParser.Parse("""
            ###
            https://example.org/wp/v2/posts
            """);

        document.Requests[0].Method.Should().Be("GET");
        document.Requests[0].Url.Should().Be("https://example.org/wp/v2/posts");
    }

    [Fact]
    public void Parse_BodyLinesAreNotParsedAsVariablesOrComments()
    {
        var document = HttpDocumentParser.Parse("""
            ###
            POST /wp/v2/posts
            Content-Type: application/json

            {
              "@context": "#hash",
              "note": "// не комментарий"
            }
            """);

        document.Requests[0].Body.Should().Contain("\"@context\": \"#hash\"");
        document.Requests[0].Body.Should().Contain("// не комментарий");
        document.Variables.Should().BeEmpty();
    }

    [Fact]
    public void Parse_MultilineJsonBodyKeepsFormatting()
    {
        var document = HttpDocumentParser.Parse("""
            ###
            POST /x

            {
              "a": 1,
              "b": [1, 2]
            }
            """);

        document.Requests[0].Body.Should().Be("{\n  \"a\": 1,\n  \"b\": [1, 2]\n}");
    }

    [Fact]
    public void Parse_RequestWithoutRequestLineThrows()
    {
        var parse = () => HttpDocumentParser.Parse("""
            ###
            просто текст без запроса
            """);

        // «просто текст» принимается за строку запроса и не разбирается
        parse.Should().Throw<HttpDocumentException>().WithMessage("*не разобрать строку запроса*");
    }

    [Fact]
    public void Parse_BlockWithOnlyCommentsGivesNoRequest()
    {
        var document = HttpDocumentParser.Parse("""
            ###
            # только комментарий

            ###
            GET /a
            """);

        document.Requests.Should().ContainSingle();
    }

    [Fact]
    public void Parse_EmptyTextGivesEmptyDocument()
    {
        HttpDocumentParser.Parse(null).Requests.Should().BeEmpty();
        HttpDocumentParser.Parse("   ").Requests.Should().BeEmpty();
    }

    [Fact]
    public void Parse_RawKeepsBlockText()
    {
        var document = HttpDocumentParser.Parse("""
            ###
            # @name createPost
            POST /wp/v2/posts
            Content-Type: application/json

            {"title":"x"}
            """);

        document.Requests[0].Raw.Should().Be("POST /wp/v2/posts\nContent-Type: application/json\n\n{\"title\":\"x\"}");
    }

    [Fact]
    public void Expand_AppliesDocumentAndRequestVariables()
    {
        var document = HttpDocumentParser.Parse("""
            @host = https://example.org

            ###
            @id = 42
            GET {{host}}/wp/v2/posts/{{id}}
            Accept: {{format}}
            """);

        var expanded = HttpDocumentParser.Expand(document, document.Requests[0],
            new Dictionary<string, string?> { ["format"] = "application/json" });

        expanded.Url.Should().Be("https://example.org/wp/v2/posts/42");
        expanded.Headers[0].Value.Should().Be("application/json");
    }

    [Fact]
    public void Expand_ExternalValuesOverrideDocument()
    {
        var document = HttpDocumentParser.Parse("""
            @perPage = 10

            ###
            GET /wp/v2/posts?per_page={{perPage}}
            """);

        var expanded = HttpDocumentParser.Expand(document, document.Requests[0],
            new Dictionary<string, string?> { ["perPage"] = "50" });

        expanded.Url.Should().Be("/wp/v2/posts?per_page=50");
    }

    [Fact]
    public void Expand_NullExternalValueKeepsDocumentVariable()
    {
        var document = HttpDocumentParser.Parse("""
            @perPage = 10

            ###
            GET /wp/v2/posts?per_page={{perPage}}
            """);

        var expanded = HttpDocumentParser.Expand(document, document.Requests[0],
            new Dictionary<string, string?> { ["perPage"] = null });

        expanded.Url.Should().Be("/wp/v2/posts?per_page=10");
    }

    [Fact]
    public void Expand_UnknownVariableThrows()
    {
        var document = HttpDocumentParser.Parse("""
            ###
            GET /wp/v2/posts/{{missing}}
            """);

        var expand = () => HttpDocumentParser.Expand(document, document.Requests[0]);

        expand.Should().Throw<HttpDocumentException>().WithMessage("*Не задана переменная \"missing\"*");
    }

    [Fact]
    public void Expand_NestedVariablesAreResolved()
    {
        var document = HttpDocumentParser.Parse("""
            @host = https://example.org
            @api = {{host}}/wp-json

            ###
            GET {{api}}/wp/v2/posts
            """);

        var expanded = HttpDocumentParser.Expand(document, document.Requests[0]);

        expanded.Url.Should().Be("https://example.org/wp-json/wp/v2/posts");
    }

    [Fact]
    public void Expand_VariableCycleThrows()
    {
        var document = HttpDocumentParser.Parse("""
            @a = {{b}}
            @b = {{a}}

            ###
            GET /x/{{a}}
            """);

        var expand = () => HttpDocumentParser.Expand(document, document.Requests[0]);

        expand.Should().Throw<HttpDocumentException>().WithMessage("*вложенность*");
    }

    [Fact]
    public void Expand_SystemVariablesGiveValues()
    {
        var document = HttpDocumentParser.Parse("""
            ###
            GET /x/{{$guid}}/{{$timestamp}}/{{$isoTimestamp}}
            """);

        var expanded = HttpDocumentParser.Expand(document, document.Requests[0]);

        var parts = expanded.Url.Split('/', StringSplitOptions.RemoveEmptyEntries);
        Guid.Parse(parts[1]).Should().NotBeEmpty();
        long.Parse(parts[2]).Should().BeGreaterThan(1_600_000_000);
        DateTimeOffset.Parse(parts[3]).Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void Expand_UnknownSystemVariableThrows()
    {
        var document = HttpDocumentParser.Parse("""
            ###
            GET /x/{{$processEnv}}
            """);

        var expand = () => HttpDocumentParser.Expand(document, document.Requests[0]);

        expand.Should().Throw<HttpDocumentException>().WithMessage("*Неизвестная системная переменная*");
    }

    [Fact]
    public void Expand_BodyAndHeadersAreExpanded()
    {
        var document = HttpDocumentParser.Parse("""
            @title = Привет

            ###
            POST /wp/v2/posts
            X-Title: {{title}}

            {"title":"{{title}}"}
            """);

        var expanded = HttpDocumentParser.Expand(document, document.Requests[0]);

        expanded.Headers[0].Value.Should().Be("Привет");
        expanded.Body.Should().Be("""{"title":"Привет"}""");
    }

    [Theory]
    [InlineData("GET", false)]
    [InlineData("HEAD", false)]
    [InlineData("OPTIONS", false)]
    [InlineData("POST", true)]
    [InlineData("PUT", true)]
    [InlineData("PATCH", true)]
    [InlineData("DELETE", true)]
    public void IsWrite_DependsOnMethod(string method, bool expected)
    {
        var document = HttpDocumentParser.Parse($"###\n{method} /x");

        document.Requests[0].IsWrite.Should().Be(expected);
    }

    /// <summary>
    /// Границы блока сервер обязан считать так же, как редактор на фронте: дерево переходит к запросу
    /// по строке из каталога, а «выполнить» ищет блок под курсором через <see cref="DocumentText"/>.
    /// </summary>
    [Fact]
    public void Parse_BlockBoundsMatchDocumentText()
    {
        const string text = """
            @host = https://example.org

            ###
            # @name posts
            GET {{host}}/wp-json/wp/v2/posts

            ### trailing
            POST {{host}}/wp-json/wp/v2/posts
            Content-Type: application/json

            {"title":"x"}

            """;

        var document = HttpDocumentParser.Parse(text);
        var blocks = DocumentText.Blocks(text).ToList();

        document.Requests.Select(request => (request.Line, request.EndLine))
            .Should().Equal(blocks.Select(block => (block.StartLine, block.EndLine)));
    }
}
