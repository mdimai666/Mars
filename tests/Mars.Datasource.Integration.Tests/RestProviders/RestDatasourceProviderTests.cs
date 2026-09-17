using System.Net;
using System.Text;
using FluentAssertions;
using Mars.Datasource.Abstractions.Interfaces;
using Mars.Datasource.Contracts.Models;
using Mars.Datasource.Host.Services;
using Mars.Datasource.Providers.Rest;
using Mars.Storage.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Datasource.Integration.Tests.RestProviders;

/// <summary>
/// Rest-источник целиком: каталог из discovery и документа запросов, сохранение каталога
/// в data-корень, выполнение запросов.
/// </summary>
public class RestDatasourceProviderTests
{
    const string PostsJson = """[{"id":1,"title":"a"},{"id":2,"title":"b"}]""";

    [Fact]
    public async Task Catalog_RunsDiscoveryAndSavesCatalog()
    {
        var source = new RestSource();

        var catalog = await source.Provider.Catalog();

        source.Discovery.Calls.Should().Be(1);
        catalog.Kind.Should().Be(DatasourceKind.Rest);
        catalog.SourceName.Should().Be("WordPress");
        catalog.Groups.Select(group => group.Name).Should().Equal("wp/v2");
        // Пути операций идут от корня REST API: запрос уходит на /wp-json/wp/v2/posts, а не на /wp/v2/posts
        catalog.Groups[0].Objects.Select(obj => obj.Id).Should().Equal("GET /wp-json/wp/v2/posts");

        source.Storage.FileExists($"datasource/{RestSource.Slug}/{DatasourceSettings.CatalogDocument}").Should().BeTrue();
    }

    [Fact]
    public async Task Catalog_SecondReadTakesSavedCatalog()
    {
        var source = new RestSource();

        await source.Provider.Catalog();
        source.Discovery.Calls.Should().Be(1);

        // Тот же источник, новый провайдер: каталог уже сохранён, в сеть идти не нужно.
        var catalog = await source.NewProvider().Catalog();

        source.Discovery.Calls.Should().Be(1);
        catalog.Groups[0].Objects.Select(obj => obj.Id).Should().Equal("GET /wp-json/wp/v2/posts");
    }

    [Fact]
    public async Task Catalog_SavedCatalogIsReusedAfterBrokenDiscovery()
    {
        var source = new RestSource();

        await source.Provider.Catalog();

        source.Discovery.Handler = _ => throw new HttpRequestException("сеть недоступна");

        var catalog = await source.NewProvider().Catalog();

        catalog.Groups.Select(group => group.Name).Should().Equal("wp/v2");
    }

    [Fact]
    public async Task Catalog_AddsDocumentRequests()
    {
        var source = new RestSource(document: """
            @perPage = 5

            ###
            # @name моиПосты
            GET {{baseUrl}}/wp/v2/posts?per_page={{perPage}}
            """);

        var catalog = await source.Provider.Catalog();

        var group = catalog.Groups.Single(group => group.Name == RestDatasourceProvider.DocumentGroupName);
        var operation = group.Objects.Single();

        operation.Id.Should().Be("моиПосты");
        operation.ObjectType.Should().Be(DatasourceObjectType.Operation);
        operation.DefaultLanguage.Should().Be(DatasourceLanguage.Http);
        operation.DefaultQuery.Should().Be("GET {{baseUrl}}/wp/v2/posts?per_page={{perPage}}");
    }

    [Fact]
    public async Task Catalog_DocumentRequestCarriesBlockLines()
    {
        // Границы блока нужны редактору: дерево переходит к запросу, «выполнить» берёт блок под курсором
        var source = new RestSource(document: """
            ###
            # @name posts
            GET /wp/v2/posts
            """);

        var catalog = await source.Provider.Catalog();

        var operation = catalog.Groups.Single(group => group.Name == RestDatasourceProvider.DocumentGroupName).Objects.Single();

        operation.Line.Should().Be(1);
        operation.EndLine.Should().Be(3);
    }

    [Fact]
    public async Task Catalog_DiscoveryOperationHasNoBlockLines()
    {
        var source = new RestSource();

        var catalog = await source.Provider.Catalog();

        var operation = catalog.Groups.Single(group => group.Name == "wp/v2").Objects.Single();

        operation.Line.Should().Be(0);
        operation.EndLine.Should().Be(0);
    }

    [Fact]
    public async Task Catalog_UnnamedDocumentRequestUsesItsTitle()
    {
        var source = new RestSource(document: """
            ###
            GET /wp/v2/users
            """);

        var catalog = await source.Provider.Catalog();

        catalog.Groups.Single(group => group.Name == RestDatasourceProvider.DocumentGroupName)
            .Objects.Single().Id.Should().Be("GET /wp/v2/users");
    }

    [Fact]
    public async Task Catalog_BrokenDocumentDoesNotBreakSource()
    {
        var source = new RestSource(document: """
            ###
            просто текст без запроса
            """);

        var catalog = await source.Provider.Catalog();

        var operation = catalog.Groups.Single(group => group.Name == RestDatasourceProvider.DocumentGroupName).Objects.Single();
        operation.Name.Should().Contain("не разобрать строку запроса");
    }

    [Fact]
    public async Task Catalog_DiscoveryNoneSkipsNetwork()
    {
        var source = new RestSource(settings: new Dictionary<string, string>
        {
            [DatasourceSettings.Discovery] = RestDiscovery.None,
        });

        var catalog = await source.Provider.Catalog();

        source.Discovery.Calls.Should().Be(0);
        source.Http.Requests.Should().BeEmpty();
        catalog.Groups.Should().BeEmpty();
    }

    [Fact]
    public async Task Catalog_UnregisteredDiscoveryIsNotSupported()
    {
        var source = new RestSource();

        // Способ сбора каталога в настройках задан, а его сборщик не подключён
        var provider = new RestDatasourceProvider(source.Config, new DatasourceStore(source.Storage),
            new FakeClientCache(source.Http), []);

        var catalog = () => provider.Catalog();

        await catalog.Should().ThrowAsync<NotSupportedException>().WithMessage($"*{RestDiscovery.WordPress}*");
    }

    [Fact]
    public async Task Catalog_WithoutBaseUrlReportsMissingAddress()
    {
        var source = new RestSource(baseUrl: "");

        var catalog = () => source.Provider.Catalog();

        await catalog.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*Не задан адрес API источника \"{RestSource.Slug}\"*");
    }

    [Fact]
    public async Task Discover_RefetchesAndOverwritesSavedCatalog()
    {
        var source = new RestSource();

        await source.Provider.Catalog();

        source.Discovery.Handler = _ => Task.FromResult<IReadOnlyList<DatasourceCatalogGroup>>(
        [
            new DatasourceCatalogGroup
            {
                Name = "wp/v2",
                Objects = [RestCatalogOperation.Operation("GET", "/wp/v2/pages", [])],
            },
        ]);

        var catalog = await source.Provider.Discover();

        source.Discovery.Calls.Should().Be(2);
        catalog.Groups.Single(group => group.Name == "wp/v2").Objects.Select(obj => obj.Id).Should().Equal("GET /wp/v2/pages");

        // Сохранённый каталог заменён: новый провайдер видит уже страницы.
        var reread = await source.NewProvider().Catalog();
        reread.Groups.Single(group => group.Name == "wp/v2").Objects.Select(obj => obj.Id).Should().Equal("GET /wp/v2/pages");
    }

    [Fact]
    public async Task Query_SendsRequestFromText()
    {
        var source = new RestSource();
        source.Http.Respond = _ => RestSource.Ok(PostsJson);

        var result = await source.Provider.Query(new DatasourceRequest
        {
            Language = DatasourceLanguage.Http,
            Query = "GET {{baseUrl}}/wp/v2/posts",
        });

        var request = source.Http.Requests.Single();
        request.Method.Should().Be(HttpMethod.Get);
        request.Url.Should().Be("https://example.org/wp/v2/posts");

        result.Ok.Should().BeTrue();
        result.Columns.Select(column => column.Name).Should().Equal("id", "title");
        result.Rows.Should().HaveCount(2);
        result.Command.Should().Be("GET {{baseUrl}}/wp/v2/posts");
    }

    [Fact]
    public async Task Query_UsesObjectIdWhenTextIsEmpty()
    {
        var source = new RestSource();
        source.Http.Respond = _ => RestSource.Ok("[]");

        await source.Provider.Query(new DatasourceRequest
        {
            Language = DatasourceLanguage.Http,
            ObjectId = "GET /wp/v2/posts/{id}",
            Parameters = [new DatasourceParam { Name = "id", Value = "42" }],
        });

        source.Http.Requests.Single().Url.Should().Be("https://example.org/wp/v2/posts/42");
    }

    [Fact]
    public async Task Query_RunsNamedRequestFromDocument()
    {
        var source = new RestSource(document: """
            ###
            # @name first
            GET /wp/v2/users

            ###
            # @name second
            GET /wp/v2/posts
            """);
        source.Http.Respond = _ => RestSource.Ok("[]");

        await source.Provider.Query(new DatasourceRequest
        {
            Language = DatasourceLanguage.Http,
            ObjectId = "second",
            Query = await source.DocumentTextAsync(),
        });

        source.Http.Requests.Single().Url.Should().Be("https://example.org/wp/v2/posts");
    }

    [Fact]
    public async Task Query_RunsFirstRequestOfDocument()
    {
        var source = new RestSource(document: """
            ###
            GET /wp/v2/users
            """);
        source.Http.Respond = _ => RestSource.Ok("[]");

        await source.Provider.Query(new DatasourceRequest
        {
            Language = DatasourceLanguage.Http,
            Query = await source.DocumentTextAsync(),
        });

        source.Http.Requests.Single().Url.Should().Be("https://example.org/wp/v2/users");
    }

    [Fact]
    public async Task Query_AppendsParametersToUrl()
    {
        var source = new RestSource();
        source.Http.Respond = _ => RestSource.Ok("[]");

        await source.Provider.Query(new DatasourceRequest
        {
            Language = DatasourceLanguage.Http,
            Query = "GET /wp/v2/posts",
            Parameters = [new DatasourceParam { Name = "per_page", Value = "10" }],
        });

        var request = source.Http.Requests.Single();
        request.Url.Should().Be("https://example.org/wp/v2/posts?per_page=10");
    }

    [Fact]
    public async Task Query_RunsDraftRequestOfCatalogOperation()
    {
        // Заготовка операции из каталога должна работать как есть: с корнем REST API в пути
        var source = new RestSource();
        source.Http.Respond = _ => RestSource.Ok(PostsJson);

        var catalog = await source.Provider.Catalog();
        var posts = catalog.Groups.SelectMany(group => group.Objects).Single(obj => obj.Id == "GET /wp-json/wp/v2/posts");

        var result = await source.Provider.Query(new DatasourceRequest
        {
            Language = DatasourceLanguage.Http,
            ObjectId = posts.Id,
            Query = posts.DefaultQuery!,
        });

        source.Http.Requests.Single().Url.Should().Be("https://example.org/wp-json/wp/v2/posts");
        result.Ok.Should().BeTrue(result.Message);
        result.Rows.Should().HaveCount(2);
    }

    [Fact]
    public async Task Query_BaseUrlWithSharedPrefixIsNotDoubled()
    {
        var source = new RestSource(baseUrl: "https://example.org/wp-json");
        source.Http.Respond = _ => RestSource.Ok("[]");

        await source.Provider.Query(new DatasourceRequest
        {
            Language = DatasourceLanguage.Http,
            Query = "GET {{baseUrl}}/wp-json/wp/v2/posts",
        });

        source.Http.Requests.Single().Url.Should().Be("https://example.org/wp-json/wp/v2/posts");
    }

    [Fact]
    public async Task Query_EmptyTextAndObjectFails()
    {
        var source = new RestSource();

        var result = await source.Provider.Query(new DatasourceRequest { Language = DatasourceLanguage.Http });

        result.Ok.Should().BeFalse();
        result.Message.Should().Contain("Пустой запрос");
        source.Http.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Query_BrokenDocumentFails()
    {
        var source = new RestSource();

        var result = await source.Provider.Query(new DatasourceRequest
        {
            Language = DatasourceLanguage.Http,
            Query = "###\nGET",
        });

        result.Ok.Should().BeFalse();
        result.Message.Should().Contain("не разобрать строку запроса");
    }

    [Fact]
    public async Task Query_HttpErrorIsReported()
    {
        var source = new RestSource();
        source.Http.Respond = _ => RestSource.Of(HttpStatusCode.Unauthorized, """{"code":"rest_not_logged_in"}""");

        var result = await source.Provider.Query(new DatasourceRequest
        {
            Language = DatasourceLanguage.Http,
            Query = "GET /wp/v2/users/me",
        });

        result.Ok.Should().BeFalse();
        result.Message.Should().Contain("401").And.Contain("rest_not_logged_in");
    }

    [Fact]
    public async Task Query_NetworkErrorIsReported()
    {
        var source = new RestSource();
        source.Http.Respond = _ => throw new HttpRequestException("имя не разрешается");

        var result = await source.Provider.Query(new DatasourceRequest
        {
            Language = DatasourceLanguage.Http,
            Query = "GET /wp/v2/posts",
        });

        result.Ok.Should().BeFalse();
        result.Message.Should().Contain("имя не разрешается");
    }

    [Fact]
    public async Task Modify_SendsWriteRequestAndReportsStatus()
    {
        var source = new RestSource();
        source.Http.Respond = _ => RestSource.Of(HttpStatusCode.Created, """{"id":7}""");

        var result = await source.Provider.Modify(new DatasourceRequest
        {
            Language = DatasourceLanguage.Http,
            Query = "POST /wp/v2/posts",
            Parameters = [new DatasourceParam { Name = "title", Value = "Новый" }],
        });

        var request = source.Http.Requests.Single();
        request.Method.Should().Be(HttpMethod.Post);
        request.Body.Should().Contain("\"title\": \"Новый\"");
        request.ContentType.Should().Be("application/json");

        result.Ok.Should().BeTrue();
        result.Message.Should().Contain("201").And.Contain("POST /wp/v2/posts");
        result.DatabaseDriver.Should().Be(DatasourceKind.Rest);
    }

    [Fact]
    public async Task Modify_HttpErrorIsNotOk()
    {
        var source = new RestSource();
        source.Http.Respond = _ => RestSource.Of(HttpStatusCode.Forbidden, "нельзя");

        var result = await source.Provider.Modify(new DatasourceRequest
        {
            Language = DatasourceLanguage.Http,
            Query = "DELETE /wp/v2/posts/1",
        });

        result.Ok.Should().BeFalse();
        result.Message.Should().Contain("403");
    }

    [Fact]
    public void Capabilities_AllowQueryBrowseAndWrite()
    {
        var capabilities = new RestSource().Provider.Capabilities;

        capabilities.CanQuery.Should().BeTrue();
        capabilities.CanBrowse.Should().BeTrue();
        capabilities.CanWrite.Should().BeTrue();
        capabilities.CanManageViews.Should().BeFalse();
    }

    [Fact]
    public void Kind_AndLanguageAreContractValues()
    {
        DatasourceKind.Rest.Should().Be("rest");
        DatasourceLanguage.Http.Should().Be("http");
    }

    [Fact]
    public void AddDatasourceRest_RegistersFactoryAndDiscoveries()
    {
        ServiceCollection services = new();
        services.AddDatasourceRest();
        services.AddSingleton<IDatasourceStore, FakeStore>();

        using var provider = services.BuildServiceProvider();

        var factory = provider.GetServices<IDatasourceProviderFactory>().Single(item => item.Kind == DatasourceKind.Rest);

        factory.Driver.Should().Be("");
        factory.DisplayName.Should().Contain("REST");
        factory.DefaultConnectionString.Should().Be("");

        provider.GetServices<IRestCatalogDiscovery>()
            .Select(discovery => discovery.Mode)
            .Should().BeEquivalentTo([RestDiscovery.WordPress, RestDiscovery.OpenApi]);
    }

    class FakeStore : IDatasourceStore
    {
        public bool Exists(string slug, string name) => false;

        public Task<string?> ReadTextAsync(string slug, string name, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);

        public Task WriteTextAsync(string slug, string name, string content, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}

/// <summary>Источник с подменными сетью, discovery и хранилищем.</summary>
class RestSource
{
    public const string Slug = "wp";

    public RestSource(string? document = null, Dictionary<string, string>? settings = null, string baseUrl = "https://example.org")
    {
        Storage = new InMemoryFileStorage();

        if (document is not null)
        {
            Storage.Write($"datasource/{Slug}/{DatasourceSettings.RequestsDocument}", new MemoryStream(Encoding.UTF8.GetBytes(document)));
        }

        Dictionary<string, string> values = new()
        {
            [DatasourceSettings.BaseUrl] = baseUrl,
            [DatasourceSettings.Discovery] = RestDiscovery.WordPress,
        };

        foreach (var (name, value) in settings ?? []) values[name] = value;

        Config = new DatasourceConfig
        {
            Kind = DatasourceKind.Rest,
            Slug = Slug,
            Title = "WordPress",
            Settings = values,
        };

        Provider = NewProvider();
    }

    public InMemoryFileStorage Storage { get; }

    public RecordingHandler Http { get; } = new();

    public FakeRestDiscovery Discovery { get; } = new();

    public DatasourceConfig Config { get; }

    public RestDatasourceProvider Provider { get; }

    public RestDatasourceProvider NewProvider() => new(Config, new DatasourceStore(Storage), new FakeClientCache(Http), [Discovery]);

    public async Task<string> DocumentTextAsync()
        => await new DatasourceStore(Storage).ReadTextAsync(Slug, DatasourceSettings.RequestsDocument) ?? "";

    public static HttpResponseMessage Ok(string body) => Of(HttpStatusCode.OK, body);

    public static HttpResponseMessage Of(HttpStatusCode status, string body) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json"),
    };
}

class FakeRestDiscovery : IRestCatalogDiscovery
{
    const string Index = """
        {
          "namespace": "wp/v2",
          "routes": {
            "/wp/v2/posts": {
              "namespace": "wp/v2",
              "endpoints": [ { "methods": ["GET"], "args": { "per_page": { "type": "integer", "default": 10 } } } ]
            }
          }
        }
        """;

    public string Mode { get; set; } = RestDiscovery.WordPress;

    public int Calls { get; private set; }

    public Func<string, Task<IReadOnlyList<DatasourceCatalogGroup>>> Handler { get; set; }
        = address => Task.FromResult(WordPressRestDiscovery.Build(Index, address));

    public Task<IReadOnlyList<DatasourceCatalogGroup>> DiscoverAsync(HttpClient client, string address,
        CancellationToken cancellationToken = default)
    {
        Calls++;

        return Handler(address);
    }
}

class FakeClientCache(RecordingHandler handler) : RestHttpClientCache
{
    readonly HttpClient _client = new(handler, disposeHandler: false);

    public override HttpClient Get(RestSourceSettings settings) => _client;
}

/// <summary>Записывает снимок запроса: сам <see cref="HttpRequestMessage"/> провайдер освобождает.</summary>
class RecordingHandler : HttpMessageHandler
{
    public List<RecordedRequest> Requests { get; } = [];

    public Func<RecordedRequest, HttpResponseMessage> Respond { get; set; } =
        _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]") };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var recorded = new RecordedRequest
        {
            Method = request.Method,
            Url = request.RequestUri?.ToString() ?? "",
            Body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken),
            ContentType = request.Content?.Headers.ContentType?.MediaType,
            Headers = request.Headers.ToDictionary(header => header.Key, header => string.Join(", ", header.Value), StringComparer.OrdinalIgnoreCase),
        };

        Requests.Add(recorded);

        return Respond(recorded);
    }
}

class RecordedRequest
{
    public HttpMethod Method { get; init; } = HttpMethod.Get;
    public string Url { get; init; } = "";
    public string Body { get; init; } = "";
    public string? ContentType { get; init; }
    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();
}
