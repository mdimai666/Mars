using AutoFixture;
using Mars.SiteEngine.Controllers;
using Mars.Data.Entities;
using Mars.Cms.Abstractions.Dto.Posts;
using Mars.Cms.Abstractions.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.SiteEngine.Services;
using Mars.Cms.Contracts.PostTypes;
using Mars.Test.Common.FixtureCustomizes;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebApiClient.Integration.Tests.Tests.PageRenders;

public class PageRenderTests : BaseWebApiClientTests
{
    public PageRenderTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        TestFrontHelper.EnsureFront(AppFixture.ServiceProvider);
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
        _ = nameof(PageRenderController.RenderPageBySlug);
        _ = nameof(PageRenderService.RenderPageBySlug);
        var client = GetWebApiClient();

        // тип "page" больше не создаётся в сиде — готовим данные сами
        var ef = AppFixture.MarsDbContext();
        var pageType = _fixture.Create<PostTypeEntity>();
        pageType.TypeName = "page";
        pageType.Statuses = PostStatusEntity.DefaultStatuses();
        pageType.EnabledFeatures = [PostTypeConstants.Features.Content];
        ef.PostTypes.Add(pageType);

        var post = _fixture.Create<PostEntity>();
        post.PostTypeId = pageType.Id;
        post.StatusId = null;
        ef.Posts.Add(post);
        await ef.SaveChangesAsync();
        ef.ChangeTracker.Clear();

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
        result.Data.Should().NotBeNull();
    }

}
