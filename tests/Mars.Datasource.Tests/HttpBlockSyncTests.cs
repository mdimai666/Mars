using FluentAssertions;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;

namespace Mars.Datasource.Tests;

/// <summary>
/// Синхронизация формы параметров с текстом блока .http: поиск операции по запросу блока,
/// чтение значений из текста и запись их обратно. Правила одни для фронта и тестов.
/// </summary>
public class HttpBlockSyncTests
{
    //=== строка запроса =========================================================

    [Fact]
    public void RequestOf_SkipsCommentsNamesAndVariables()
    {
        var request = HttpBlockSync.RequestOf("""
            ### posts
            # @name posts
            @perPage = 10

            GET {{baseUrl}}/wp/v2/posts?per_page={{perPage}} HTTP/1.1
            Accept: application/json
            """);

        request.Should().NotBeNull();
        request!.Value.Method.Should().Be("GET");
        request.Value.Url.Should().Be("{{baseUrl}}/wp/v2/posts?per_page={{perPage}}");
    }

    [Fact]
    public void RequestOf_UrlOnlyLineIsGet()
    {
        var request = HttpBlockSync.RequestOf("/api/items");

        request.Should().NotBeNull();
        request!.Value.Method.Should().Be("GET");
        request.Value.Url.Should().Be("/api/items");
    }

    [Fact]
    public void RequestOf_NoRequestGivesNull()
    {
        HttpBlockSync.RequestOf("# только комментарий").Should().BeNull();
        HttpBlockSync.RequestOf("").Should().BeNull();
    }

    //=== поиск операции ========================================================

    [Fact]
    public void MatchOperation_FindsByMethodAndPathTail()
    {
        var posts = Op("GET", "/wp/v2/posts", ("per_page", DatasourceParameterIn.Query));
        var post = Op("GET", "/wp/v2/posts/{id}", ("id", DatasourceParameterIn.Path));

        HttpBlockSync.MatchOperation("GET {{baseUrl}}/wp/v2/posts?per_page=10", [posts, post]).Should().BeSameAs(posts);
        HttpBlockSync.MatchOperation("GET {{baseUrl}}/wp/v2/posts/7", [posts, post]).Should().BeSameAs(post);
    }

    [Fact]
    public void MatchOperation_AbsoluteUrlWithPrefixMatchesTail()
    {
        var post = Op("GET", "/wp/v2/posts/{id}", ("id", DatasourceParameterIn.Path));

        HttpBlockSync.MatchOperation("GET https://site.org/wp-json/wp/v2/posts/7", [post]).Should().BeSameAs(post);
    }

    [Fact]
    public void MatchOperation_TemplateMatchesDocumentVariable()
    {
        var post = Op("GET", "/wp/v2/posts/{id}", ("id", DatasourceParameterIn.Path));

        HttpBlockSync.MatchOperation("GET {{baseUrl}}/wp/v2/posts/{{postId}}", [post]).Should().BeSameAs(post);
    }

    [Fact]
    public void MatchOperation_MethodMustMatch()
    {
        var posts = Op("GET", "/wp/v2/posts", ("per_page", DatasourceParameterIn.Query));

        HttpBlockSync.MatchOperation("POST {{baseUrl}}/wp/v2/posts", [posts]).Should().BeNull();
    }

    [Fact]
    public void MatchOperation_ObjectsWithoutParametersAreNotFormOwners()
    {
        // Запрос пользователя из документа: метод известен, схемы параметров нет — формы у блока нет.
        var documentRequest = new DatasourceCatalogObject
        {
            Id = "my request",
            Operation = new DatasourceOperation { Method = "GET" },
        };

        HttpBlockSync.MatchOperation("GET {{baseUrl}}/anything", [documentRequest]).Should().BeNull();
    }

    [Fact]
    public void MatchOperation_PrefersMoreSpecificPath()
    {
        var generic = Op("GET", "/wp/v2/{type}/{id}", ("type", DatasourceParameterIn.Path), ("id", DatasourceParameterIn.Path));
        var posts = Op("GET", "/wp/v2/posts/{id}", ("id", DatasourceParameterIn.Path));

        HttpBlockSync.MatchOperation("GET {{baseUrl}}/wp/v2/posts/7", [generic, posts]).Should().BeSameAs(posts);
    }

    //=== чтение значений ========================================================

    [Fact]
    public void ReadValues_QueryPathHeaderBody()
    {
        var operation = Op("POST", "/wp/v2/posts/{id}",
            ("id", DatasourceParameterIn.Path),
            ("context", DatasourceParameterIn.Query),
            ("X-WP-Nonce", DatasourceParameterIn.Header),
            ("title", DatasourceParameterIn.Body),
            ("count", DatasourceParameterIn.Body));

        var values = HttpBlockSync.ReadValues("""
            POST {{baseUrl}}/wp/v2/posts/7?context=edit
            X-WP-Nonce: abc123

            {
              "title": "hello",
              "count": 5
            }
            """, operation);

        values.Should().BeEquivalentTo(new Dictionary<string, string?>
        {
            ["id"] = "7",
            ["context"] = "edit",
            ["X-WP-Nonce"] = "abc123",
            ["title"] = "hello",
            ["count"] = "5",
        });
    }

    [Fact]
    public void ReadValues_VariableStaysAsIs_AndMissingIsAbsent()
    {
        var operation = Op("GET", "/wp/v2/posts/{id}",
            ("id", DatasourceParameterIn.Path),
            ("per_page", DatasourceParameterIn.Query));

        var values = HttpBlockSync.ReadValues("GET {{baseUrl}}/wp/v2/posts/{{postId}}", operation);

        values.Should().ContainKey("id").WhoseValue.Should().Be("{{postId}}");
        values.Should().NotContainKey("per_page");
    }

    //=== запись значений ========================================================

    [Fact]
    public void ApplyValues_UpdatesQueryKeepsOthersAndVersion()
    {
        var operation = Op("GET", "/wp/v2/posts", ("per_page", DatasourceParameterIn.Query));

        var updated = HttpBlockSync.ApplyValues(
            "GET {{baseUrl}}/wp/v2/posts?per_page=10&search=x HTTP/1.1",
            operation,
            new Dictionary<string, string?> { ["per_page"] = "20" });

        updated.Should().Be("GET {{baseUrl}}/wp/v2/posts?per_page=20&search=x HTTP/1.1");
    }

    [Fact]
    public void ApplyValues_EmptyValueRemovesQueryKey_AndAddsMissing()
    {
        var operation = Op("GET", "/wp/v2/posts",
            ("per_page", DatasourceParameterIn.Query),
            ("page", DatasourceParameterIn.Query));

        var updated = HttpBlockSync.ApplyValues(
            "GET {{baseUrl}}/wp/v2/posts?per_page=10",
            operation,
            new Dictionary<string, string?> { ["per_page"] = "", ["page"] = "2" });

        updated.Should().Be("GET {{baseUrl}}/wp/v2/posts?page=2");
    }

    [Fact]
    public void ApplyValues_ExpandsPathTemplateIncludingDraftVariable()
    {
        var operation = Op("GET", "/wp/v2/posts/{id}", ("id", DatasourceParameterIn.Path));

        HttpBlockSync.ApplyValues("GET {{baseUrl}}/wp/v2/posts/{id}", operation,
            new Dictionary<string, string?> { ["id"] = "7" })
            .Should().Be("GET {{baseUrl}}/wp/v2/posts/7");

        HttpBlockSync.ApplyValues("GET {{baseUrl}}/wp/v2/posts/{{id}}", operation,
            new Dictionary<string, string?> { ["id"] = "7" })
            .Should().Be("GET {{baseUrl}}/wp/v2/posts/7");
    }

    [Fact]
    public void ApplyValues_KeepsBlockFurniture()
    {
        var operation = Op("GET", "/wp/v2/posts", ("per_page", DatasourceParameterIn.Query));

        var updated = HttpBlockSync.ApplyValues("""
            ### posts
            # @name posts
            @host = https://site.org

            GET {{baseUrl}}/wp/v2/posts
            Accept: application/json
            """, operation, new Dictionary<string, string?> { ["per_page"] = "10" });

        Normalize(updated).Should().Be(Normalize("""
            ### posts
            # @name posts
            @host = https://site.org

            GET {{baseUrl}}/wp/v2/posts?per_page=10
            Accept: application/json
            """));
    }

    [Fact]
    public void ApplyValues_SetsAndRemovesBodyProperties()
    {
        var operation = Op("POST", "/wp/v2/posts",
            ("title", DatasourceParameterIn.Body),
            ("status", DatasourceParameterIn.Body));

        var updated = HttpBlockSync.ApplyValues("""
            POST {{baseUrl}}/wp/v2/posts
            Content-Type: application/json

            {
              "title": "old",
              "keep": true
            }
            """, operation, new Dictionary<string, string?> { ["title"] = "new", ["status"] = "" });

        updated.Should().Contain("\"title\": \"new\"");
        updated.Should().Contain("\"keep\": true");
        updated.Should().NotContain("status");
    }

    [Fact]
    public void ApplyValues_CreatesBodyAfterBlankLine()
    {
        var operation = Op("POST", "/wp/v2/posts", ("title", DatasourceParameterIn.Body));

        var updated = HttpBlockSync.ApplyValues("POST {{baseUrl}}/wp/v2/posts",
            operation, new Dictionary<string, string?> { ["title"] = "hello" });

        Normalize(updated).Should().Be(Normalize("""
            POST {{baseUrl}}/wp/v2/posts

            {
              "title": "hello"
            }
            """));
    }

    [Fact]
    public void ApplyValues_NonJsonBodyIsNotTouched()
    {
        var operation = Op("POST", "/upload", ("title", DatasourceParameterIn.Body));

        var text = """
            POST {{baseUrl}}/upload
            Content-Type: text/plain

            обычный текст
            """;

        Normalize(HttpBlockSync.ApplyValues(text, operation, new Dictionary<string, string?> { ["title"] = "x" }))
            .Should().Be(Normalize(text));
    }

    [Fact]
    public void RoundTrip_FormValuesSurviveWriteAndRead()
    {
        var operation = Op("POST", "/wp/v2/posts/{id}",
            ("id", DatasourceParameterIn.Path),
            ("context", DatasourceParameterIn.Query),
            ("title", DatasourceParameterIn.Body));

        Dictionary<string, string?> values = new() { ["id"] = "7", ["context"] = "edit", ["title"] = "hello" };

        var text = HttpBlockSync.ApplyValues("###\nPOST {{baseUrl}}/wp/v2/posts/{id}", operation, values);
        var read = HttpBlockSync.ReadValues(text, operation);

        read.Should().BeEquivalentTo(values);
    }

    static string Normalize(string text) => text.Replace("\r\n", "\n").TrimEnd('\n');

    static DatasourceCatalogObject Op(string method, string path, params (string Name, string In)[] parameters) => new()
    {
        Id = $"{method} {path}",
        Name = $"{method} {path}",
        ObjectType = DatasourceObjectType.Operation,
        DefaultLanguage = DatasourceLanguage.Http,
        Operation = new DatasourceOperation
        {
            Method = method,
            Parameters = parameters
                .Select(parameter => new DatasourceOperationParameter { Name = parameter.Name, In = parameter.In })
                .ToList(),
        },
    };
}
