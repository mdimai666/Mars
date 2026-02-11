using System.Web;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Services;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Renders;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers.PageRenders;

public class GetPageRenderTests : ApplicationTests
{
    const string _apiUrl = "/api/PageRender";

    public GetPageRenderTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    private async Task<PostSummary> GetPostFirstByType(string type)
    {
        var ps = AppFixture.ServiceProvider.GetRequiredService<IPostService>();
        var items = await ps.List(new() { Type = type, Take = 1 }, default);
        return items.Items.First();
    }

    [IntegrationFact]
    public async Task RenderPostById_Request_Success()
    {
        //Arrange
        _ = nameof(PageRenderController.RenderById);
        _ = nameof(PageRenderService.RenderPostById);
        var client = AppFixture.GetClient();

        var post = await GetPostFirstByType("post");

        //Act
        var res = await client.Request(_apiUrl, "by-id", post.Id).AllowAnyHttpStatus().GetAsync();
        var result = await res.GetJsonAsync<RenderActionResult<PostRenderResponse>>();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Data.Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task RenderPostBySlug_Request_Success()
    {
        //Arrange
        _ = nameof(PageRenderController.RenderPost);
        _ = nameof(PageRenderService.RenderPostBySlug);
        var client = AppFixture.GetClient();

        var post = await GetPostFirstByType("post");

        //Act
        var res = await client.Request(_apiUrl, "by-post", post.Type, post.Slug).AllowAnyHttpStatus().GetAsync();
        var result = await res.GetJsonAsync<RenderActionResult<PostRenderResponse>>();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Data.Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task RenderPageBySlug_Request_Success()
    {
        //Arrange
        _ = nameof(PageRenderController.RenderPageBySlug);
        _ = nameof(PageRenderService.RenderPageBySlug);
        var client = AppFixture.GetClient();

        var post = await GetPostFirstByType("page");

        //Act
        var res = await client.Request(_apiUrl, "by-slug", post.Slug).AllowAnyHttpStatus().GetAsync();
        var result = await res.GetJsonAsync<RenderActionResult<PostRenderResponse>>();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Data.Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task RenderUrl_SimpleRequest_Success()
    {
        //Arrange
        _ = nameof(PageRenderController.RenderUrl);
        _ = nameof(PageRenderService.RenderUrl);
        var client = AppFixture.GetClient();

        var url = HttpUtility.UrlEncode("/admin");

        //Act
        var res = await client.Request(_apiUrl, "by-url").AppendQueryParam(new { url }).AllowAnyHttpStatus().GetAsync();
        var result = await res.GetJsonAsync<RenderActionResult<PostRenderResponse>>();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Data.Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task RenderUrl_NonExistUrl_ShouldStatus200Instead404()
    {
        //Arrange
        _ = nameof(PageRenderController.RenderUrl);
        _ = nameof(PageRenderService.RenderUrl);
        var client = AppFixture.GetClient();

        var url = HttpUtility.UrlEncode($"/zuzu/{Guid.NewGuid()}");

        //Act
        var res = await client.Request(_apiUrl, "by-url").AppendQueryParam(new { url }).AllowAnyHttpStatus().GetAsync();
        var result = await res.GetJsonAsync<RenderActionResult<PostRenderResponse>>();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Data.Should().NotBeNull();
    }
}
