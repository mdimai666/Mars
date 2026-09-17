using FluentAssertions;
using Mars.Datasource.Abstractions.Interfaces;
using Mars.Datasource.Contracts.Models;
using Mars.Datasource.Providers.Rest;
using Mars.Integration.Tests.Attributes;

namespace ExternalServices.Integration.Tests.WordPressTests;

/// <summary>
/// Rest-источник на живом WordPress: discovery по индексу <c>wp-json</c>, чтение постов таблицей,
/// доступ через Basic Auth и запись. Скипнуты, как остальные тесты стенда: нужен Docker, интернет и git.
/// </summary>
public class WordPressDatasourceTests : IClassFixture<WordPressFixture>
{
    readonly WordPressFixture _fixture;

    public WordPressDatasourceTests(WordPressFixture fixture)
    {
        _fixture = fixture;
    }

    RestDatasourceProvider Provider(string authMode = RestAuthMode.None) => new(
        new DatasourceConfig
        {
            Kind = DatasourceKind.Rest,
            Slug = "wp",
            Title = "WordPress stand",
            Settings = new Dictionary<string, string>
            {
                [DatasourceSettings.BaseUrl] = _fixture.WordPressUrl,
                [DatasourceSettings.Discovery] = RestDiscovery.WordPress,
                [DatasourceSettings.AuthMode] = authMode,
                [DatasourceSettings.AuthUsername] = "admin",
                [DatasourceSettings.AuthPassword] = "admin",
            },
        },
        new MemoryStore(),
        new RestHttpClientCache(),
        [new WordPressRestDiscovery()]);

    static DatasourceRequest Request(string query, params DatasourceParam[] parameters) => new()
    {
        Language = DatasourceLanguage.Http,
        Query = query,
        Parameters = parameters.Length == 0 ? null : parameters.ToList(),
        MaxRows = 50,
    };

    static DatasourceParam Param(string name, string? value) => new() { Name = name, Value = value };

    [IntegrationFact(Skip = WordPressPerformanceTests.SkipTest)]
    public async Task Rest_DiscoveryBuildsCatalogFromWordPressIndex()
    {
        var catalog = await Provider().Discover();

        catalog.Kind.Should().Be(DatasourceKind.Rest);
        // Дерево в списке разложено по методам, а не по namespace описания API
        catalog.Groups.Select(group => group.Name).Should().Contain("GET");

        var operations = catalog.Groups.SelectMany(group => group.Objects).ToList();

        // Маршруты индекса идут от корня REST API — в каталоге путь полный
        var posts = operations.Single(operation => operation.Id == "GET /wp-json/wp/v2/posts");
        posts.ObjectType.Should().Be(DatasourceObjectType.Operation);
        posts.DefaultLanguage.Should().Be(DatasourceLanguage.Http);
        posts.DefaultQuery.Should().Be("GET {{baseUrl}}/wp-json/wp/v2/posts");
        posts.Parameters.Select(parameter => parameter.Name).Should().Contain("per_page");

        var post = operations.Single(operation => operation.Id == "GET /wp-json/wp/v2/posts/{id}");
        post.Parameters.Should().Contain(parameter => parameter.Name == "id" && parameter.In == DatasourceParameterIn.Path);
    }

    [IntegrationFact(Skip = WordPressPerformanceTests.SkipTest)]
    public async Task Rest_DraftRequestOfCatalogOperationWorks()
    {
        // Заготовка из дерева должна работать как есть: без корня REST API запрос уходит в 404
        var provider = Provider();
        var catalog = await provider.Discover();
        var posts = catalog.Groups.SelectMany(group => group.Objects).Single(obj => obj.Id == "GET /wp-json/wp/v2/posts");

        var result = await provider.Query(Request(posts.DefaultQuery! + "?per_page=2&_fields=id,slug"));

        result.Ok.Should().BeTrue(result.Message);
        result.Rows.Should().HaveCount(2);
    }

    [IntegrationFact(Skip = WordPressPerformanceTests.SkipTest)]
    public async Task Rest_CatalogIsSavedAndReused()
    {
        var store = new MemoryStore();
        var provider = new RestDatasourceProvider(Config(), store, new RestHttpClientCache(), [new WordPressRestDiscovery()]);

        var discovered = await provider.Discover();

        store.Exists("wp", DatasourceSettings.CatalogDocument).Should().BeTrue();

        // Каталог сохранён: повторное чтение не ходит в индекс и отдаёт те же операции.
        var reread = await new RestDatasourceProvider(Config(), store, new RestHttpClientCache(), [new WordPressRestDiscovery()]).Catalog();

        reread.Groups.SelectMany(group => group.Objects).Select(obj => obj.Id)
            .Should().BeEquivalentTo(discovered.Groups.SelectMany(group => group.Objects).Select(obj => obj.Id));
    }

    [IntegrationFact(Skip = WordPressPerformanceTests.SkipTest)]
    public async Task Rest_QueryReadsPostsAsTable()
    {
        var result = await Provider().Query(Request("GET {{baseUrl}}/wp-json/wp/v2/posts?per_page=3&_fields=id,slug,status"));

        result.Ok.Should().BeTrue(result.Message);
        result.Columns.Select(column => column.Name).Should().Contain(["id", "slug", "status"]);
        result.Rows.Should().HaveCount(3);
        result.Truncated.Should().BeFalse();

        // WordPress сообщает общее число записей заголовком — оно уходит в результат.
        result.Total.Should().BeGreaterThanOrEqualTo(3);
    }

    [IntegrationFact(Skip = WordPressPerformanceTests.SkipTest)]
    public async Task Rest_QueryParametersGoToQueryString()
    {
        var result = await Provider().Query(Request("GET {{baseUrl}}/wp-json/wp/v2/posts",
            Param("per_page", "2"), Param("_fields", "id,slug")));

        result.Ok.Should().BeTrue(result.Message);
        result.Rows.Should().HaveCount(2);
    }

    [IntegrationFact(Skip = WordPressPerformanceTests.SkipTest)]
    public async Task Rest_QuerySinglePostKeepsDocument()
    {
        var list = await Provider().Query(Request("GET {{baseUrl}}/wp-json/wp/v2/posts?per_page=1&_fields=id"));
        var id = list.Rows[0][0];

        var result = await Provider().Query(Request("GET {{baseUrl}}/wp-json/wp/v2/posts/{id}", Param("id", id)));

        result.Ok.Should().BeTrue(result.Message);
        result.Rows.Should().ContainSingle();
        result.Json.Should().Contain("\"id\"");
    }

    [IntegrationFact(Skip = WordPressPerformanceTests.SkipTest)]
    public async Task Rest_BasicAuthReadsCurrentUser()
    {
        var result = await Provider(RestAuthMode.Basic).Query(Request("GET {{baseUrl}}/wp-json/wp/v2/users/me?context=edit"));

        result.Ok.Should().BeTrue(result.Message);
        result.Json.Should().Contain("admin");
    }

    [IntegrationFact(Skip = WordPressPerformanceTests.SkipTest)]
    public async Task Rest_WithoutAuthPrivateRequestFails()
    {
        var result = await Provider().Query(Request("GET {{baseUrl}}/wp-json/wp/v2/users/me?context=edit"));

        result.Ok.Should().BeFalse();
        result.Message.Should().Contain("401");
    }

    [IntegrationFact(Skip = WordPressPerformanceTests.SkipTest)]
    public async Task Rest_WriteCreatesAndDeletesPost()
    {
        var provider = Provider(RestAuthMode.Basic);

        var created = await provider.Query(Request("POST {{baseUrl}}/wp-json/wp/v2/posts",
            Param("title", "Mars datasource test"), Param("status", "draft"), Param("content", "created by test")));

        created.Ok.Should().BeTrue(created.Message);

        var id = created.Columns.Select((column, index) => (column.Name, index))
            .Single(item => item.Name == "id").index;
        var postId = created.Rows[0][id]!;

        var deleted = await provider.Modify(Request("DELETE {{baseUrl}}/wp-json/wp/v2/posts/{id}?force=true",
            Param("id", postId)));

        deleted.Ok.Should().BeTrue(deleted.Message);
        deleted.Message.Should().Contain("200");
    }

    DatasourceConfig Config() => new()
    {
        Kind = DatasourceKind.Rest,
        Slug = "wp",
        Title = "WordPress stand",
        Settings = new Dictionary<string, string>
        {
            [DatasourceSettings.BaseUrl] = _fixture.WordPressUrl,
            [DatasourceSettings.Discovery] = RestDiscovery.WordPress,
        },
    };

    /// <summary>Тело источника в памяти: data-корень тестовому стенду не нужен.</summary>
    class MemoryStore : IDatasourceStore
    {
        readonly Dictionary<string, string> _files = new(StringComparer.OrdinalIgnoreCase);

        public bool Exists(string slug, string name) => _files.ContainsKey($"{slug}/{name}");

        public Task<string?> ReadTextAsync(string slug, string name, CancellationToken cancellationToken = default)
            => Task.FromResult(_files.TryGetValue($"{slug}/{name}", out var content) ? content : null);

        public Task WriteTextAsync(string slug, string name, string content, CancellationToken cancellationToken = default)
        {
            _files[$"{slug}/{name}"] = content;

            return Task.CompletedTask;
        }
    }
}
