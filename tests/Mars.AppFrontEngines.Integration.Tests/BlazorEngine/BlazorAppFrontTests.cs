using FluentAssertions;
using Flurl.Http;
using Mars.AppFrontEngines.Integration.Tests.Common;
using Mars.Host.Handlers;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite.Interfaces;
using Mars.Integration.Tests.Attributes;
using Mars.Test.Common.FixtureCustomizes;
using Mars.UseStartup;
using Mars.WebSiteProcessor.Blazor;
using Mars.WebSiteProcessor.Interfaces;
using Mars.WebSiteProcessor.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.AppFrontEngines.Integration.Tests.BlazorEngine;

/// <summary>
/// <see cref="BlazorWebRenderEngine"/>
/// </summary>
public class BlazorAppFrontTests : BaseAppFrontTests<BlazorAppFrontApplicationFixture>, IDefaultRenderEngineTests
{
    public BlazorAppFrontTests(BlazorAppFrontApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());

        _ = nameof(StartupFront);
        _ = nameof(BlazorWebRenderEngine);
        _ = nameof(WebTemplateService.ScanSite);
        var app = AppFixture.ServiceProvider.GetRequiredService<IWebRenderEngineLocator>().GetAppFrontForUrl("/")!;
        app.Features.Get<IWebTemplateService>().ScanSite();
    }

    [IntegrationFact(Skip = "Blazor-рендер отложен — движок не регистрируется до отдельной задачи")]
    public async Task Basic_IndexPage_ShouldOk()
    {
        //Arrange
        _ = nameof(InitialSiteDataViewModelHandler.Handle);
        //_ = nameof(BlazorTemplateExample.IndexPage);
        //_ = nameof(Mars.WebApp.Pages._Host);
        var expectText = "Hello, world! from Blazor!";

        //Act
        var render = await RenderRequestPage("/");

        //Assert
        //render.Should().Contain(UserConstants.TestUserFirstName);
        render.Should().Contain(expectText);
    }

    [IntegrationFact(Skip = "Blazor-рендер отложен — движок не регистрируется до отдельной задачи")]
    public async Task Basic_SecondPage_ShouldOk()
    {
        //Arrange
        var expectText = "SecondPage";

        //Act
        var render = await RenderRequestPage("/second");

        //Assert
        render.Should().Contain(expectText);
    }

    [IntegrationFact(Skip = "Blazor-рендер отложен — движок не регистрируется до отдельной задачи")]
    public async Task Basic_Page404_ShouldStatusCode404()
    {
        //Act
        // appTheme/pages/404.hbs
        var (html, status) = await RenderRequestPageEx("/non_exist_pageUrl_for_404");

        //Assert
        html.Should().Contain("Page not found in Blazor app");
        status.Should().Be(StatusCodes.Status404NotFound);
    }

    [IntegrationFact(Skip = "Blazor-рендер отложен — движок не регистрируется до отдельной задачи")]
    public async Task Basic_IsPrerenderAppFlagIsSetup_ShouldReturnTrue()
    {
        //Act
        var expectText = "IsPrerenderProcess=true";

        // appTheme/pages/404.hbs
        var html = await RenderRequestPage("/prerender_flag");

        //Assert
        html.Should().Contain(expectText);
    }

    public void UserNametest()
    {
        throw new NotImplementedException("TODO: Q is static. Static is problem for multi thread render");
    }

    [IntegrationFact(Skip = "Blazor-рендер отложен — движок не регистрируется до отдельной задачи")]
    public async Task Basic_PlacedWwwrootFileWillServe_FileIsAvailableAtTheLink()
    {
        //Arrange
        //_webTemplateService.Template.Returns(EmptyWebSiteTemplate("index_page"));

        //Act
        var client = AppFixture.GetClient();
        var res = await client.Request("style.css").GetAsync();
        var fileContent = await res.GetStringAsync();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        fileContent.Trim().Should().Contain("padding: 0;");
    }

    [IntegrationFact(Skip = "Blazor-рендер отложен — движок не регистрируется до отдельной задачи")]
    public async Task Basic_RootFilesDontWillServe_FilesFailAtTheLink()
    {
        //Act
        var client = AppFixture.GetClient();
        var (html, status) = await RenderRequestPageEx("/BlazorTemplateExample.dll");

        //Assert
        status.Should().Be(StatusCodes.Status404NotFound);
        html.Should().BeNullOrEmpty();
    }

}
