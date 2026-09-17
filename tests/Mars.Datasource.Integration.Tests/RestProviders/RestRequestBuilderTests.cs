using FluentAssertions;
using Mars.Datasource.Contracts.Models;
using Mars.Datasource.Providers.Rest;

namespace Mars.Datasource.Integration.Tests.RestProviders;

/// <summary>
/// HTTP-запрос из текста и параметров: переменные <c>{{name}}</c>, шаблоны пути <c>{name}</c>,
/// строка запроса для чтения и JSON-тело для записи.
/// </summary>
public class RestRequestBuilderTests
{
    static RestSourceSettings Settings(string baseUrl = "https://example.org")
        => RestSourceSettings.From(new DatasourceConfig
        {
            Kind = DatasourceKind.Rest,
            Slug = "wp",
            Settings = new Dictionary<string, string> { [DatasourceSettings.BaseUrl] = baseUrl },
        });

    static HttpRequestMessage Build(string text, RestSourceSettings? settings = null, params DatasourceParam[] parameters)
    {
        var document = HttpDocumentParser.Parse(text);

        return RestRequestBuilder.Build(document, document.Requests[0], settings ?? Settings(), parameters);
    }

    static DatasourceParam Param(string name, string? value) => new() { Name = name, Value = value };

    static async Task<string> BodyAsync(HttpRequestMessage message)
        => message.Content is null ? "" : await message.Content.ReadAsStringAsync();

    [Fact]
    public void Build_RelativeUrlUsesBaseUrl()
    {
        using var message = Build("GET /wp/v2/posts");

        message.RequestUri!.ToString().Should().Be("https://example.org/wp/v2/posts");
        message.Method.Should().Be(HttpMethod.Get);
    }

    [Fact]
    public void Build_BaseUrlVariableIsProvided()
    {
        using var message = Build("GET {{baseUrl}}/wp/v2/posts");

        message.RequestUri!.ToString().Should().Be("https://example.org/wp/v2/posts");
    }

    [Fact]
    public void Build_BaseUrlVariableFromParameterWins()
    {
        using var message = Build("GET {{baseUrl}}/x", parameters: Param("baseUrl", "https://other.org"));

        message.RequestUri!.ToString().Should().Be("https://other.org/x");
    }

    [Fact]
    public void Build_AbsoluteUrlIsKept()
    {
        using var message = Build("GET https://other.org/x");

        message.RequestUri!.ToString().Should().Be("https://other.org/x");
    }

    [Fact]
    public void Build_PathTemplateTakesParameterValue()
    {
        using var message = Build("GET /wp/v2/posts/{id}", parameters: Param("id", "42"));

        message.RequestUri!.AbsolutePath.Should().Be("/wp/v2/posts/42");
    }

    [Fact]
    public void Build_QueryParametersAreAppended()
    {
        using var message = Build("GET /wp/v2/posts", parameters: [Param("per_page", "10"), Param("page", "2")]);

        message.RequestUri!.Query.Should().Contain("per_page=10").And.Contain("page=2");
    }

    [Fact]
    public void Build_ParameterMentionedInTextIsNotAppendedTwice()
    {
        using var message = Build("GET /wp/v2/posts?per_page={{per_page}}", parameters: Param("per_page", "10"));

        message.RequestUri!.Query.Should().Be("?per_page=10");
    }

    [Fact]
    public void Build_ParameterAlreadyInUrlIsNotAppended()
    {
        using var message = Build("GET /wp/v2/posts?per_page=5", parameters: Param("per_page", "10"));

        message.RequestUri!.Query.Should().Be("?per_page=5");
    }

    [Fact]
    public void Build_ParameterValueIsEscaped()
    {
        using var message = Build("GET /wp/v2/posts", parameters: Param("search", "a b&c"));

        message.RequestUri!.Query.Should().Be("?search=a%20b%26c");
    }

    [Fact]
    public async Task Build_WriteWithoutBodyBuildsJsonBody()
    {
        using var message = Build("POST /wp/v2/posts", parameters: [Param("title", "Привет"), Param("count", "3"), Param("flag", "true")]);

        message.Method.Should().Be(HttpMethod.Post);

        var body = await BodyAsync(message);
        body.Should().Contain("\"title\": \"Привет\"");
        body.Should().Contain("\"count\": 3");
        body.Should().Contain("\"flag\": true");

        message.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task Build_WriteWithBodyKeepsBody()
    {
        using var message = Build("""
            POST /wp/v2/posts
            Content-Type: application/json

            {"title":"из документа"}
            """, parameters: Param("title", "из формы"));

        (await BodyAsync(message)).Should().Be("""{"title":"из документа"}""");
        message.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public void Build_HeadersAreApplied()
    {
        using var message = Build("""
            GET /wp/v2/posts
            Accept: application/json
            X-WP-Nonce: 123
            """);

        message.Headers.Accept.ToString().Should().Be("application/json");
        message.Headers.GetValues("X-WP-Nonce").Should().Equal("123");
    }

    [Fact]
    public async Task Build_ContentTypeFromDocumentIsKept()
    {
        using var message = Build("""
            POST /x
            Content-Type: text/plain

            просто текст
            """);

        message.Content!.Headers.ContentType!.MediaType.Should().Be("text/plain");
        (await BodyAsync(message)).Should().Be("просто текст");
    }

    [Fact]
    public void Build_MethodIsKept()
    {
        using var message = Build("DELETE /wp/v2/posts/42");

        message.Method.Should().Be(HttpMethod.Delete);
        message.RequestUri!.AbsolutePath.Should().Be("/wp/v2/posts/42");
    }

    [Fact]
    public void Build_MissingVariableThrows()
    {
        var build = () => Build("GET /x/{{missing}}");

        build.Should().Throw<HttpDocumentException>().WithMessage("*missing*");
    }

    [Fact]
    public void Build_QueryParameterWithoutValueBecomesEmpty()
    {
        using var message = Build("GET /x", parameters: Param("search", null));

        message.RequestUri!.Query.Should().Be("?search=");
    }

    [Fact]
    public void Build_UrlWithoutBaseUrlSettingStaysRelative()
    {
        using var message = Build("GET /x", Settings(baseUrl: ""));

        message.RequestUri!.IsAbsoluteUri.Should().BeFalse();
    }

    [Fact]
    public void Settings_DiscoveryAddressIsBuiltFromBaseUrl()
    {
        var settings = RestSourceSettings.From(new DatasourceConfig
        {
            Kind = DatasourceKind.Rest,
            Slug = "wp",
            Settings = new Dictionary<string, string> { [DatasourceSettings.BaseUrl] = "https://example.org/" },
        });

        settings.Discovery.Should().Be(RestDiscovery.WordPress);
        settings.DiscoveryAddress.Should().Be("https://example.org/wp-json/wp/v2");
    }

    [Fact]
    public void Settings_DiscoveryUrlOverridesDefault()
    {
        var settings = RestSourceSettings.From(new DatasourceConfig
        {
            Kind = DatasourceKind.Rest,
            Slug = "api",
            Settings = new Dictionary<string, string>
            {
                [DatasourceSettings.BaseUrl] = "https://example.org",
                [DatasourceSettings.Discovery] = RestDiscovery.OpenApi,
                [DatasourceSettings.DiscoveryUrl] = "/openapi.json",
            },
        });

        settings.DiscoveryAddress.Should().Be("https://example.org/openapi.json");
    }

    [Fact]
    public void Settings_UnknownDiscoveryFallsBackToNone()
    {
        var settings = RestSourceSettings.From(new DatasourceConfig
        {
            Kind = DatasourceKind.Rest,
            Slug = "api",
            Settings = new Dictionary<string, string> { [DatasourceSettings.Discovery] = "graphql" },
        });

        settings.Discovery.Should().Be(RestDiscovery.None);
    }

    [Fact]
    public void Settings_AuthIsBuiltFromSettings()
    {
        var settings = RestSourceSettings.From(new DatasourceConfig
        {
            Kind = DatasourceKind.Rest,
            Slug = "wp",
            Settings = new Dictionary<string, string>
            {
                [DatasourceSettings.BaseUrl] = "https://example.org",
                [DatasourceSettings.AuthMode] = RestAuthMode.Basic,
                [DatasourceSettings.AuthUsername] = "admin",
                [DatasourceSettings.AuthPassword] = "secret",
            },
        });

        settings.Auth.Should().NotBeNull();
        settings.Auth!.Mode.Should().Be(Mars.HttpSmartAuthFlow.AuthMode.BasicAuth);
        settings.Auth.Username.Should().Be("admin");
        settings.Auth.Id.Should().Be("datasource-wp");
    }

    [Fact]
    public void Settings_WithoutAuthModeHasNoAuth()
    {
        var settings = RestSourceSettings.From(new DatasourceConfig { Kind = DatasourceKind.Rest, Slug = "wp" });

        settings.Auth.Should().BeNull();
        settings.TimeoutSec.Should().Be(RestSourceSettings.DefaultTimeoutSec);
    }
}
