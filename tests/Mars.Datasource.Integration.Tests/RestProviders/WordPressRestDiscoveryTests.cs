using System.Net;
using FluentAssertions;
using Mars.Datasource.Contracts.Models;
using Mars.Datasource.Providers.Rest;

namespace Mars.Datasource.Integration.Tests.RestProviders;

/// <summary>
/// Каталог REST API WordPress из индекса <c>/wp-json/wp/v2</c>: группы по пространствам имён,
/// операции на метод, аргументы как параметры и префикс <c>/wp-json</c> в путях.
/// </summary>
public class WordPressRestDiscoveryTests
{
    /// <summary>Адрес индекса namespace: маршруты внутри него заданы без корня REST API.</summary>
    const string Address = "https://example.org/wp-json/wp/v2";

    /// <summary>Форма ответа индекса WordPress: routes → endpoints → args.</summary>
    const string Index = """
    {
      "name": "Test blog",
      "namespace": "wp/v2",
      "namespaces": ["wp/v2", "wp-site-health/v1"],
      "routes": {
        "/wp/v2/posts": {
          "namespace": "wp/v2",
          "methods": ["GET", "POST"],
          "endpoints": [
            {
              "methods": ["GET"],
              "args": {
                "context": { "required": false, "default": "view", "enum": ["view", "embed", "edit"], "description": "Scope", "type": "string" },
                "page": { "required": false, "default": 1, "minimum": 1, "type": "integer", "description": "Current page" },
                "per_page": { "required": false, "default": 10, "maximum": 100, "type": "integer" },
                "search": { "required": false, "type": "string" }
              }
            },
            {
              "methods": ["POST"],
              "args": {
                "title": { "type": "string", "required": false, "description": "The title for the object" },
                "status": { "type": "string", "default": "draft" }
              }
            }
          ]
        },
        "/wp/v2/posts/(?P<id>[\\d]+)": {
          "namespace": "wp/v2",
          "endpoints": [
            { "methods": ["GET", "PUT", "DELETE"], "args": { "id": { "type": "integer", "required": false } } }
          ]
        },
        "/wp-site-health/v1/tests/background-updates": {
          "namespace": "wp-site-health/v1",
          "endpoints": [ { "methods": ["GET"], "args": {} } ]
        }
      }
    }
    """;

    static IReadOnlyList<DatasourceCatalogGroup> Build(string? address = Address)
        => WordPressRestDiscovery.Build(Index, address);

    static DatasourceCatalogObject Object(IReadOnlyList<DatasourceCatalogGroup> groups, string id)
        => groups.SelectMany(group => group.Objects).Single(obj => obj.Id == id);

    [Fact]
    public void Build_GroupsByNamespace()
    {
        var groups = Build();

        groups.Select(group => group.Name).Should().Equal("wp-site-health/v1", "wp/v2");
        groups.Single(group => group.Name == "wp/v2").Objects.Should().HaveCount(5);
    }

    [Fact]
    public void Build_OperationPerMethod()
    {
        var groups = Build();

        groups.SelectMany(group => group.Objects)
            .Select(obj => obj.Id)
            .Should()
            .BeEquivalentTo(
            [
                "GET /wp-json/wp/v2/posts",
                "POST /wp-json/wp/v2/posts",
                "DELETE /wp-json/wp/v2/posts/{id}",
                "GET /wp-json/wp/v2/posts/{id}",
                "PUT /wp-json/wp/v2/posts/{id}",
                "GET /wp-json/wp-site-health/v1/tests/background-updates",
            ]);
    }

    [Fact]
    public void Build_RouteRegexBecomesPathParameter()
    {
        var groups = Build();

        var id = Object(groups, "GET /wp-json/wp/v2/posts/{id}").Parameters.Single(parameter => parameter.Name == "id");

        id.In.Should().Be(DatasourceParameterIn.Path);
        id.Required.Should().BeTrue();
    }

    [Fact]
    public void Build_ReadArgsAreQueryParameters()
    {
        var groups = Build();

        var parameters = Object(groups, "GET /wp-json/wp/v2/posts").Parameters;

        parameters.Select(parameter => parameter.Name).Should().Equal("context", "page", "per_page", "search");
        parameters.Should().OnlyContain(parameter => parameter.In == DatasourceParameterIn.Query);

        var context = parameters.Single(parameter => parameter.Name == "context");
        context.Type.Should().Be("string");
        context.Default.Should().Be("view");
        context.Enum.Should().Equal("view", "embed", "edit");
        context.Description.Should().Be("Scope");

        parameters.Single(parameter => parameter.Name == "page").Default.Should().Be("1");
    }

    [Fact]
    public void Build_WriteArgsAreBodyParameters()
    {
        var groups = Build();

        var parameters = Object(groups, "POST /wp-json/wp/v2/posts").Parameters;

        parameters.Select(parameter => parameter.Name).Should().Equal("title", "status");
        parameters.Should().OnlyContain(parameter => parameter.In == DatasourceParameterIn.Body);
        parameters.Single(parameter => parameter.Name == "status").Default.Should().Be("draft");
    }

    [Fact]
    public void Build_OperationCarriesHttpDraftWithRealPath()
    {
        var groups = Build();

        var operation = Object(groups, "GET /wp-json/wp/v2/posts/{id}");

        operation.ObjectType.Should().Be(DatasourceObjectType.Operation);
        operation.DefaultLanguage.Should().Be(DatasourceLanguage.Http);
        operation.DefaultQuery.Should().Be("GET {{baseUrl}}/wp-json/wp/v2/posts/{{id}}");
        operation.Columns.Should().BeEmpty();
    }

    [Fact]
    public void Build_IndexRootWithoutNamespaceSegmentKeepsItInPrefix()
    {
        // Корневой индекс /wp-json: маршруты всё равно идут от /wp-json
        WordPressRestDiscovery.Build(Index, "https://example.org/wp-json")
            .SelectMany(group => group.Objects).Select(obj => obj.Id)
            .Should().Contain("GET /wp-json/wp/v2/posts");
    }

    [Fact]
    public void Build_CustomRestRootIsKept()
    {
        var groups = WordPressRestDiscovery.Build("""
            {
              "namespace": "custom/v1",
              "routes": {
                "/custom/v1/items": {
                  "namespace": "custom/v1",
                  "endpoints": [ { "methods": ["GET"], "args": {} } ]
                }
              }
            }
            """, "https://example.org/api/v1/custom/v1");

        groups.SelectMany(group => group.Objects).Select(obj => obj.Id)
            .Should().Equal("GET /api/v1/custom/v1/items");
    }

    [Fact]
    public void Build_WithoutAddressKeepsRoutePaths()
    {
        // Разбор индекса без адреса — пути как в ответе (провайдер всегда передаёт адрес)
        Build(address: null).SelectMany(group => group.Objects).Select(obj => obj.Id)
            .Should().Contain("GET /wp/v2/posts");
    }

    [Fact]
    public void Build_NamespaceFromRoutesIsUsedWhenIndexHasNone()
    {
        var groups = WordPressRestDiscovery.Build("""
            {
              "routes": {
                "/wp/v2/posts": {
                  "namespace": "wp/v2",
                  "endpoints": [ { "methods": ["GET"], "args": {} } ]
                }
              }
            }
            """, Address);

        groups.SelectMany(group => group.Objects).Select(obj => obj.Id)
            .Should().Equal("GET /wp-json/wp/v2/posts");
    }

    [Fact]
    public void Build_RouteWithoutNamespaceUsesFirstSegment()
    {
        var groups = WordPressRestDiscovery.Build("""
            { "routes": { "/custom/v1/items": { "endpoints": [ { "methods": ["GET"], "args": {} } ] } } }
            """, Address);

        groups.Single().Name.Should().Be("custom");
    }

    [Fact]
    public void Build_ResponseWithoutRoutesThrows()
    {
        var build = () => WordPressRestDiscovery.Build("""{ "name": "blog" }""", Address);

        build.Should().Throw<InvalidOperationException>().WithMessage("*нет \"routes\"*");
    }

    [Theory]
    [InlineData("https://example.org/wp-json/wp/v2", "wp/v2", "/wp-json")]
    [InlineData("https://example.org/wp-json/wp/v2/", "wp/v2", "/wp-json")]
    [InlineData("https://example.org/wp-json", "wp/v2", "/wp-json")]
    [InlineData("https://example.org/api/v1/custom/v1", "custom/v1", "/api/v1")]
    [InlineData("/v1", null, "/v1")]
    [InlineData("https://example.org/", null, "")]
    [InlineData(null, "wp/v2", "")]
    public void RoutePrefix_ComesFromDiscoveryAddress(string? address, string? ns, string expected)
    {
        RestRoutePrefix.FromAddress(address, ns).Should().Be(expected);
    }

    [Theory]
    [InlineData("/wp/v2/posts", "/wp/v2/posts")]
    [InlineData("/wp/v2/posts/(?P<id>[\\d]+)", "/wp/v2/posts/{id}")]
    [InlineData("/wp/v2/media/(?P<id>[\\d]+)", "/wp/v2/media/{id}")]
    public void NormalizePath_ConvertsRouteRegex(string route, string expected)
    {
        var (path, parameters) = WordPressRestDiscovery.NormalizePath(route);

        path.Should().Be(expected);

        if (route.Contains("(?P")) parameters.Should().Equal("id");
        else parameters.Should().BeEmpty();
    }

    [Fact]
    public async Task DiscoverAsync_FailedRequestReportsAddressAndStatus()
    {
        using var client = FakeHttp.Client(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("""{"code":"rest_not_logged_in"}"""),
        });

        var discover = () => new WordPressRestDiscovery().DiscoverAsync(client, Address);

        await discover.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*401*https://example.org/wp-json/wp/v2*rest_not_logged_in*");
    }

    [Fact]
    public async Task DiscoverAsync_ReadsIndexOverHttpAndPrefixesPaths()
    {
        string? requested = null;

        using var client = FakeHttp.Client(request =>
        {
            requested = request.RequestUri?.ToString();

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Index) };
        });

        var groups = await new WordPressRestDiscovery().DiscoverAsync(client, Address);

        requested.Should().Be(Address);
        groups.Should().HaveCount(2);
        groups.SelectMany(group => group.Objects).Select(obj => obj.Id).Should().Contain("GET /wp-json/wp/v2/posts");
    }

    [Fact]
    public void DiscoveryMode_IsWordPress()
    {
        new WordPressRestDiscovery().Mode.Should().Be(RestDiscovery.WordPress);
        RestDiscovery.DefaultUrl(RestDiscovery.WordPress).Should().Be("/wp-json/wp/v2");
    }
}

/// <summary>HttpClient с подменным обработчиком: rest-провайдер ходит в сеть, а в тестах — нет.</summary>
static class FakeHttp
{
    public static HttpClient Client(Func<HttpRequestMessage, HttpResponseMessage> respond)
        => new(new FakeHandler(respond));

    public static HttpClient Client(HttpStatusCode status, string body)
        => Client(_ => new HttpResponseMessage(status) { Content = new StringContent(body) });

    class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(respond(request));
    }
}
