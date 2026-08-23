using System.Web;
using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Services;
using Mars.Shared.Common;
using Mars.Shared.Contracts.PostTypes;
using Mars.Shared.Contracts.Renders;
using Mars.Shared.Options;
using Mars.Test.Common.FixtureCustomizes;
using Mars.Test.Common.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers.PageRenders;

public class GetPageRenderTests : ApplicationTests
{
    const string _apiUrl = "/api/PageRender";
    const string _frontSlug = "render-test";

    public GetPageRenderTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        EnsureFront();
    }

    // После реворка фронтов (file-based fronts + FrontsOption в БД) в тестовом окружении
    // фронт не создаётся (EnsureDefaultFront пропускает тесты) — регистрируем файловую тему,
    // иначе PageRender отдаёт ответ без Data (фронт для url не найден).
    private void EnsureFront()
    {
        var optionService = AppFixture.ServiceProvider.GetRequiredService<IOptionService>();
        var option = optionService.GetOption<FrontsOption>();
        if (option.Fronts.Any(s => s.Slug == _frontSlug)) return;

        var themePath = SolutionPathHelper.Resolve("tests", "Mars.Integration.Tests", "Controllers", "PageRenders", "appTheme");
        option.Fronts.Add(new FrontItem
        {
            Slug = _frontSlug,
            Title = _frontSlug,
            Url = "",
            Path = themePath,
            EngineId = FrontItem.HandlebarsEngine,
            Enabled = true,
        });
        optionService.SaveOption(option);
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

        // тип "page" больше не создаётся в сиде — готовим данные сами
        var ef = AppFixture.MarsDbContext();
        var pageType = _fixture.Create<PostTypeEntity>();
        pageType.TypeName = "page";
        pageType.Statuses = PostStatusEntity.DefaultStatuses();
        pageType.EnabledFeatures = [PostTypeConstants.Features.Content];
        ef.PostTypes.Add(pageType);

        var page = _fixture.Create<PostEntity>();
        page.PostTypeId = pageType.Id;
        page.StatusId = null;
        ef.Posts.Add(page);
        await ef.SaveChangesAsync();
        ef.ChangeTracker.Clear();

        //Act
        var res = await client.Request(_apiUrl, "by-slug", page.Slug).AllowAnyHttpStatus().GetAsync();
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
    public async Task RenderUrl_ByFrontUrl_RendersRequestedPage()
    {
        //Arrange
        _ = nameof(PageRenderController.RenderUrl);
        _ = nameof(PageRenderService.RenderUrl);
        var client = AppFixture.GetClient();

        var url = HttpUtility.UrlEncode("/");

        //Act — by-url рендерит именно запрошенный url фронта, а не путь API-эндпоинта
        var res = await client.Request(_apiUrl, "by-url").AppendQueryParam(new { url }).AllowAnyHttpStatus().GetAsync();
        var result = await res.GetJsonAsync<RenderActionResult<PostRenderResponse>>();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Ok.Should().BeTrue();
        result.NotFound.Should().BeFalse();
        result.Data.Should().NotBeNull();
        result.Data!.Html.Should().Contain("Render test front");
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
