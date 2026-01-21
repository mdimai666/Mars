using FluentAssertions;
using Mars.AppFrontEngines.Integration.Tests.Common;
using Mars.Core.Extensions;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite.Interfaces;
using Mars.Host.WebSite.SourceProviders;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Extensions;
using Mars.Shared.Contracts.PostTypes;
using Mars.Shared.Options;
using Mars.Test.Common.FixtureCustomizes;
using Mars.UseStartup;
using Mars.WebSiteProcessor.DatabaseHandlebars;
using Mars.WebSiteProcessor.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.AppFrontEngines.Integration.Tests.DatabaseHandlebarsEngine;

/// <summary>
/// <see cref="DatabaseHandlebarsWebRenderEngine"/>
/// </summary>
public class DatabaseHandlebarsAppFrontTests : BaseAppFrontTests<DatabaseHandlebarsAppFrontApplicationFixture>, IDefaultRenderEngineTests
{

    public DatabaseHandlebarsAppFrontTests(DatabaseHandlebarsAppFrontApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _ = nameof(StartupFront);
        _ = nameof(WebFilesReadFilesystemService);
        _ = nameof(WebDatabaseTemplateService.ScanSite);
        _ = nameof(WebTemplateDatabaseSource.ReadParts);
        //var app = AppFixture.ServiceProvider.GetRequiredService<IMarsAppProvider>();
        //var optionService = AppFixture.ServiceProvider.GetRequiredService<IOptionService>();
        SetupPages().RunSync();
    }

    async Task SetupPages()
    {
        var postTypeRepo = AppFixture.ServiceProvider.GetRequiredService<IPostTypeRepository>();
        var pageType = await postTypeRepo.GetDetailByName("page", default);
        var updateQuery = pageType!.CopyViaJsonConversion<UpdatePostTypeQuery>();
        await postTypeRepo.Update(updateQuery with
        {
            PostContentSettings = new PostContentSettingsDto
            {
                PostContentType = PostTypeConstants.DefaultPostContentTypes.Code,
                CodeLang = null
            }
        }, default);

        var postRepo = AppFixture.ServiceProvider.GetRequiredService<IPostRepository>();

        var indexPageId = postRepo.GetDetailBySlug("index", "page", default).RunSync().Id;
        await postRepo.Delete(indexPageId, default);

        var indexPage = _fixture.CreatePost("Hello, world! from database page!", postType: "page", slug: "index");
        await postRepo.Create(indexPage, default);

        var secondPage = _fixture.CreatePost("<h1>SecondPage</h1>", postType: "page", slug: "second");
        await postRepo.Create(secondPage, default);

        var _404Page = _fixture.CreatePost("<h1>page_404</h1>", postType: "page", slug: "404");
        await postRepo.Create(_404Page, default);

        RefreshPagesStructure();
    }

    private void RefreshPagesStructure()
    {
        var app = AppFixture.ServiceProvider.GetRequiredService<IMarsAppProvider>();
        app.FirstApp.Features.Get<IWebTemplateService>().ScanSite();
    }

    [IntegrationFact]
    public async Task Basic_IndexPage_ShouldOk()
    {
        //Arrange
        var expectText = "Hello, world! from database page!";

        //Act
        var render = await RenderRequestPage("/");

        //Assert
        //render.Should().Contain(UserConstants.TestUserFirstName);
        render.Should().Contain(expectText);
    }

    [IntegrationFact]
    public async Task Basic_SecondPage_ShouldOk()
    {
        //Arrange
        var expectText = "SecondPage";

        //Act
        var render = await RenderRequestPage("/second");

        //Assert
        render.Should().Contain(expectText);
    }

    [IntegrationFact]
    public async Task Basic_Page404_ShouldStatusCode404()
    {
        //Act
        // appTheme/pages/404.hbs
        var (html, status) = await RenderRequestPageEx("/non_exist_pageUrl_for_404");

        //Assert
        html.Should().Contain("page_404");
        status.Should().Be(StatusCodes.Status404NotFound);
    }

    [IntegrationFact]
    public async Task ResolveUrl_UseUrlFromPageContentAttribute_ShouldSuccess()
    {
        //Arrange
        var pageContent = """
            @page "/{SLUG}"

            {{#context}}
            post = ef.Posts.First(post.Slug.ToLower() == SLUG.ToLower());
            {{/context}}

            <h1>{{post.Title}}</h1>
            """;
        var pageTemplate = _fixture.CreatePost(pageContent, postType: "page", slug: "slug_non_used");
        var somePost = _fixture.CreatePost("Content", postType: "post", slug: "post1");

        var postRepo = AppFixture.ServiceProvider.GetRequiredService<IPostRepository>();
        await postRepo.Create(pageTemplate, default);
        await postRepo.Create(somePost, default);
        RefreshPagesStructure();

        //Act
        var (html, status) = await RenderRequestPageEx("/" + somePost.Slug);

        //Assert
        html.Should().Contain(somePost.Title);
        status.Should().Be(StatusCodes.Status200OK);
    }

    [IntegrationFact]
    public async Task RootHtml_UsingDefaultLayout_ShouldSuccess()
    {
        //Arrange
        var hostHtml = """
            @DefaultLayout base-layout
            <!DOCTYPE html>
            <html lang="ru">
            <head></head>
            <body>
                @Body
            </body>
            """;
        var optionService = AppFixture.ServiceProvider.GetRequiredService<IOptionService>();
        var frontOptions = optionService.GetOption<FrontOptions>();
        frontOptions.HostItems[0].HostHtml = hostHtml;
        optionService.SaveOption(frontOptions);

        var layoutContent = """
            @inherits LayoutComponentBase

            <div id="base-layout" class="container">
                <main>
                    {{>@partial-block}}
                </main>
            </div>
            """;
        var layout = _fixture.CreatePost(layoutContent, postType: "template", slug: "base-layout");

        var postRepo = AppFixture.ServiceProvider.GetRequiredService<IPostRepository>();
        await postRepo.Create(layout, default);
        RefreshPagesStructure();

        //Act
        var (html, status) = await RenderRequestPageEx("/");

        //Assert
        html.Should().Contain("id=\"base-layout\"");
        status.Should().Be(StatusCodes.Status200OK);
    }

}
