using FluentAssertions;
using Flurl.Http;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite.Interfaces;
using Mars.Host.Shared.WebSite.Models;
using Mars.Integration.Tests.AppFront;
using Mars.Integration.Tests.Attributes;
using Mars.Test.Common.Constants;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebSiteProcessor.Handlebars.TemplateData;
using Mars.WebSiteProcessor.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Mars.Integration.Tests.AppFronts.HandlebarsEngine.Scenarions;

public class MainScenarios : BaseAppFrontTests
{
    private readonly IWebTemplateService _webTemplateService;

    public MainScenarios(AppFrontApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _ = nameof(WebFilesReadFilesystemService);
        _ = nameof(WebTemplateService.ScanSite);
        var app = AppFixture.ServiceProvider.GetRequiredService<IMarsAppProvider>();
        _webTemplateService = Substitute.For<IWebTemplateService>();
        app.FirstApp.Features.Set<IWebTemplateService>(_webTemplateService);
    }

    async Task<string> RenderRequestPage(string url = "/")
    {
        var client = AppFixture.GetClient();
        return await client.Request(url).GetStringAsync(); //render index page
    }

    async Task<(string html, int statusCode)> RenderRequestPageEx(string url = "/")
    {
        var client = AppFixture.GetClient();
        var res = await client.Request(url).AllowAnyHttpStatus().GetAsync();
        var html = await res.GetStringAsync();
        return (html, res.StatusCode);
    }

    WebSiteTemplate EmptyWebSiteTemplate(string indexContent, WebPartSource[]? parts = null) =>
        new WebSiteTemplate([
            new WebPartSource("""
                <html>
                <body>
                @Body
                </body>
                </html>
                """, "_root.hbs","","",""),
            new WebPartSource("@page /\n\n" + indexContent, "index.hbs","","",""),
            ..(parts??[])
            ]);

    [IntegrationFact]
    public async Task Basic_RenderUsername_ShouldAuthUserName()
    {
        //Arrange
        _ = nameof(HandlebarsTmpCtxBasicDataContext.UserParamKey);
        var template = "{{_user.FirstName}}";
        _webTemplateService.Template.Returns(EmptyWebSiteTemplate(template));

        //Act
        var render = await RenderRequestPage();

        //Assert
        render.Should().Contain(UserConstants.TestUserFirstName);
    }

    [IntegrationFact]
    public async Task Basic_PlacedWwwrootFileWillServe_FileIsAvailableAtTheLink()
    {
        //Arrange
        _webTemplateService.Template.Returns(EmptyWebSiteTemplate("index_page"));

        //Act
        var client = AppFixture.GetClient();
        var res = await client.Request("1.txt").GetAsync();
        var fileContent = await res.GetStringAsync();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        fileContent.Trim().Should().Be("1");
    }

    [IntegrationFact]
    public async Task Basic_Page404_ShouldStatusCode404()
    {
        //Arrange
        var template = "index_page";
        var page404 = new WebPartSource("""
            @page /404

            <h1>page_404</h1>
            """, "404", "404", "", "");
        _webTemplateService.Template.Returns(EmptyWebSiteTemplate(template, [page404]));

        //Act
        var (render, statusCode) = await RenderRequestPageEx("/non_exist_pageUrl_for_404");

        //Assert
        render.Should().Contain("page_404");
        render.Should().NotContain("index_page");
        statusCode.Should().Be(StatusCodes.Status404NotFound);
    }
}
