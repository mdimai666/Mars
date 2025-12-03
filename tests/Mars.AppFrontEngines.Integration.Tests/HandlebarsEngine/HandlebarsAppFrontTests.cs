using FluentAssertions;
using Flurl.Http;
using Mars.AppFrontEngines.Integration.Tests.Common;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebSiteProcessor.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.AppFrontEngines.Integration.Tests.HandlebarsEngine;

public class HandlebarsAppFrontTests : BaseAppFrontTests<HandlebarsAppFrontApplicationFixture>, IDefaultRenderEngineTests
{
    public HandlebarsAppFrontTests(HandlebarsAppFrontApplicationFixture appFixture) : base(appFixture)
    {

        _fixture.Customize(new FixtureCustomize());
        _ = nameof(WebFilesReadFilesystemService);
        _ = nameof(WebTemplateService.ScanSite);
        var app = AppFixture.ServiceProvider.GetRequiredService<IMarsAppProvider>();
    }

    [IntegrationFact]
    public async Task Basic_IndexPage_ShouldOk()
    {
        //Arrange
        var expectText = "Hello, world! from appTheme!";

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
    public async Task Basic_PlacedWwwrootFileWillServe_FileIsAvailableAtTheLink()
    {
        //Arrange
        //_webTemplateService.Template.Returns(EmptyWebSiteTemplate("index_page"));

        //Act
        var client = AppFixture.GetClient();
        var res = await client.Request("1.txt").GetAsync();
        var fileContent = await res.GetStringAsync();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        fileContent.Trim().Should().Be("1");
    }

    [IntegrationFact]
    public async Task Basic_RootFilesDontWillServe_FilesFailAtTheLink()
    {
        //Act
        var client = AppFixture.GetClient();
        var (html, status) = await RenderRequestPageEx("/_root.hbs");

        //Assert
        status.Should().Be(StatusCodes.Status404NotFound);
        html.Should().BeNullOrEmpty();
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
}
