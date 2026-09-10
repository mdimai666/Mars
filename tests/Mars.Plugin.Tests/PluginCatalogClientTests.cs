using System.Net;
using System.Text;
using FluentAssertions;
using Mars.Core.Exceptions;
using Mars.Plugin.Contracts.Catalog;
using Mars.Plugin.Handlers;
using Mars.Plugin.Options;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using MOptions = Microsoft.Extensions.Options.Options;

namespace Mars.Plugin.Tests;

public class PluginCatalogClientTests
{
    private const string CatalogUrl = "https://catalog.test";

    private const string PluginJson = """
        {
            "packageId": "com.example.plugin",
            "displayName": "Example Plugin",
            "summary": "Example",
            "description": "Long description",
            "authorName": "Author",
            "repositoryUrl": "https://repo",
            "homepageUrl": null,
            "licenseUrl": null,
            "iconUrl": "https://icon",
            "tags": ["http", "demo"],
            "status": "recommended",
            "isRecommended": true,
            "marsVersionMin": "0.8",
            "marsVersionMax": null,
            "latestVersion": "1.2.0",
            "totalDownloads": 42,
            "avgRating": 4.5,
            "reviewsCount": 2,
            "createdAt": "2026-09-01T00:00:00Z",
            "updatedAt": "2026-09-02T00:00:00Z"
        }
        """;

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public List<Uri> Requests { get; } = [];

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);
            try
            {
                return Task.FromResult(_respond(request));
            }
            catch (Exception ex)
            {
                return Task.FromException<HttpResponseMessage>(ex);
            }
        }
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public StubHttpClientFactory(HttpMessageHandler handler) => _handler = handler;

        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
    }

    static PluginCatalogClient NewClient(HttpMessageHandler handler, bool enabled = true, string url = CatalogUrl)
        => new(new StubHttpClientFactory(handler),
               MOptions.Create(new PluginCatalogOption { Enabled = enabled, Url = url }),
               NullLogger<PluginCatalogClient>.Instance);

    static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task SearchAsync_BuildsUrlAndParsesResponse()
    {
        var handler = new StubHandler(_ => Json($$"""{"items":[{{PluginJson}}],"total":1,"page":2,"take":5}"""));
        var client = NewClient(handler);

        var result = await client.SearchAsync(new MarketplaceSearchRequest
        {
            Q = "example",
            Recommended = true,
            Sort = "rating",
            Page = 2,
            Take = 5,
        }, marsVersion: "0.8.3", CancellationToken.None);

        result.Should().NotBeNull();
        result!.Total.Should().Be(1);
        result.Page.Should().Be(2);
        result.Items.Should().HaveCount(1);

        var plugin = result.Items[0];
        plugin.PackageId.Should().Be("com.example.plugin");
        plugin.DisplayName.Should().Be("Example Plugin");
        plugin.IsRecommended.Should().BeTrue();
        plugin.Tags.Should().Equal("http", "demo");
        plugin.LatestVersion.Should().Be("1.2.0");
        plugin.TotalDownloads.Should().Be(42);
        plugin.AvgRating.Should().Be(4.5);

        var uri = handler.Requests.Single();
        uri.AbsolutePath.Should().Be("/api/plugins");
        var query = QueryHelpers.ParseQuery(uri.Query);
        query["q"].ToString().Should().Be("example");
        query["recommended"].ToString().Should().Be("true");
        query["sort"].ToString().Should().Be("rating");
        query["minVersion"].ToString().Should().Be("0.8.3");
        query["page"].ToString().Should().Be("2");
        query["take"].ToString().Should().Be("5");
    }

    [Fact]
    public async Task SearchAsync_Disabled_ReturnsNullWithoutHttp()
    {
        var handler = new StubHandler(_ => Json("{}"));
        var client = NewClient(handler, enabled: false);

        var result = await client.SearchAsync(new MarketplaceSearchRequest(), null, CancellationToken.None);

        result.Should().BeNull();
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_EmptyUrl_TreatedAsDisabled()
    {
        var handler = new StubHandler(_ => Json("{}"));
        var client = NewClient(handler, url: "");

        var result = await client.SearchAsync(new MarketplaceSearchRequest(), null, CancellationToken.None);

        result.Should().BeNull();
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAsync_ParsesPlugin()
    {
        var handler = new StubHandler(_ => Json(PluginJson));
        var client = NewClient(handler);

        var plugin = await client.GetAsync("Com.Example.Plugin", CancellationToken.None);

        plugin.Should().NotBeNull();
        plugin!.PackageId.Should().Be("com.example.plugin");
        handler.Requests.Single().AbsolutePath.Should().Be("/api/plugins/Com.Example.Plugin");
    }

    [Fact]
    public async Task GetAsync_NotFound_ReturnsNull()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = NewClient(handler);

        var plugin = await client.GetAsync("unknown.plugin", CancellationToken.None);

        plugin.Should().BeNull();
    }

    [Fact]
    public async Task GetReviewsAsync_ParsesReviews()
    {
        var handler = new StubHandler(_ => Json("""
            {"items":[
                {"id":7,"userSub":"sub1","userName":"User","rating":5,"text":"Отлично",
                 "createdAt":"2026-09-01T00:00:00Z","updatedAt":"2026-09-01T00:00:00Z"}
            ],"total":1,"page":1,"take":20}
            """));
        var client = NewClient(handler);

        var result = await client.GetReviewsAsync("com.example.plugin", 1, 20, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items[0].Rating.Should().Be(5);
        result.Items[0].UserName.Should().Be("User");
        handler.Requests.Single().AbsolutePath.Should().Be("/api/plugins/com.example.plugin/reviews");
    }

    [Fact]
    public async Task GetAsync_ServerError_Throws()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var client = NewClient(handler);

        var act = () => client.GetAsync("com.example.plugin", CancellationToken.None);

        await act.Should().ThrowAsync<UserActionException>().WithMessage("*ошибкой 500*");
    }

    [Fact]
    public async Task GetAsync_InvalidJson_Throws()
    {
        var handler = new StubHandler(_ => Json("{ not json"));
        var client = NewClient(handler);

        var act = () => client.GetAsync("com.example.plugin", CancellationToken.None);

        await act.Should().ThrowAsync<UserActionException>().WithMessage("*некорректный ответ*");
    }

    [Fact]
    public async Task GetAsync_ConnectionFailure_Throws()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("No connection could be made"));
        var client = NewClient(handler);

        var act = () => client.GetAsync("com.example.plugin", CancellationToken.None);

        await act.Should().ThrowAsync<UserActionException>().WithMessage("*недоступен*");
    }

    [Fact]
    public async Task GetAsync_Timeout_Throws()
    {
        var handler = new StubHandler(_ => throw new TaskCanceledException());
        var client = NewClient(handler);

        var act = () => client.GetAsync("com.example.plugin", CancellationToken.None);

        await act.Should().ThrowAsync<UserActionException>().WithMessage("*не ответил вовремя*");
    }
}
