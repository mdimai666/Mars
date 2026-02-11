using Mars.Controllers;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebApiClient.Integration.Tests.Tests.PageRenders;

public class PageRenderTests : BaseWebApiClientTests
{
    public PageRenderTests(ApplicationFixture appFixture) : base(appFixture)
    {

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
        var client = GetWebApiClient();
        var post = await GetPostFirstByType("post");

        //Act
        var result = await client.PageRender.Render(post.Id);

        //Assert
        result.Should().NotBeNull();
        result.Ok.Should().BeTrue();
        result.Data.Title.Should().Be(post.Title);
    }

    [IntegrationFact]
    public async Task RenderPostBySlug_Request_Success()
    {
        //Arrange
        _ = nameof(PageRenderController.RenderPost);
        _ = nameof(PageRenderService.RenderPostBySlug);
        var client = GetWebApiClient();
        var post = await GetPostFirstByType("post");

        //Act
        var result = await client.PageRender.RenderPost(post.Type, post.Slug);

        //Assert
        result.Should().NotBeNull();
        result.Ok.Should().BeTrue();
        result.Data.Title.Should().Be(post.Title);
    }

    [IntegrationFact]
    public async Task RenderPageBySlug_Request_Success()
    {
        //Arrange
        _ = nameof(PageRenderController.RenderById);
        _ = nameof(PageRenderService.RenderPageBySlug);
        var client = GetWebApiClient();
        var post = await GetPostFirstByType("page");

        //Act
        var result = await client.PageRender.Render(post.Slug);

        //Assert
        result.Should().NotBeNull();
        result.Ok.Should().BeTrue();
        result.Data.Title.Should().Be(post.Title);
    }

    [IntegrationFact]
    public async Task RenderUrl_Request_Success()
    {
        //Arrange
        _ = nameof(PageRenderController.RenderUrl);
        _ = nameof(PageRenderService.RenderUrl);
        var client = GetWebApiClient();
        var url = "/admin";

        //Act
        var result = await client.PageRender.RenderUrl(url);

        //Assert
        result.Should().NotBeNull();
        result.Ok.Should().BeTrue();
    }

}
