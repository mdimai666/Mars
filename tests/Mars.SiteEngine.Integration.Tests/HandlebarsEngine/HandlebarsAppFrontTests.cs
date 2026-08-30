using FluentAssertions;
using Flurl.Http;
using Mars.Integration.Tests.Attributes;
using Mars.Options.Abstractions.Services;
using Mars.Server.Contracts.Options;
using Mars.SiteEngine.Abstractions.Services;
using Mars.SiteEngine.Host.Services;
using Mars.SiteEngine.Integration.Tests.Common;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.SiteEngine.Integration.Tests.HandlebarsEngine;

[Collection(HandlebarsAppFrontCollection.CollectionName)]
public class HandlebarsAppFrontTests : BaseAppFrontTests<HandlebarsAppFrontApplicationFixture>, IDefaultRenderEngineTests
{
    public HandlebarsAppFrontTests(HandlebarsAppFrontApplicationFixture appFixture) : base(appFixture)
    {

        _fixture.Customize(new FixtureCustomize());
        _ = nameof(WebFilesReadFilesystemService);
        _ = nameof(WebTemplateService.ScanSite);
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
    public async Task StaticFile_ServedWithRevalidationCache_And304OnConditionalRequest()
    {
        //Arrange
        var client = AppFixture.GetClient();

        //Act
        var res = await client.Request("1.txt").SendAsync(HttpMethod.Get);

        //Assert — txt не ассет: ревалидация, а не долгий кеш
        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        res.Headers.TryGetFirst("Cache-Control", out var cacheControl).Should().BeTrue();
        cacheControl.Should().Be("no-cache");
        res.Headers.TryGetFirst("ETag", out var etag).Should().BeTrue();
        etag.Should().NotBeNullOrWhiteSpace();

        var conditional = await client.Request("1.txt")
            .WithHeader("If-None-Match", etag)
            .AllowAnyHttpStatus()
            .SendAsync(HttpMethod.Get);

        conditional.StatusCode.Should().Be(StatusCodes.Status304NotModified);
    }

    [IntegrationFact]
    public async Task StaticFile_AssetServedWithLongTermCache()
    {
        //Arrange
        var client = AppFixture.GetClient();

        //Act
        var res = await client.Request("logo.svg").SendAsync(HttpMethod.Get);

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        res.Headers.TryGetFirst("Cache-Control", out var cacheControl).Should().BeTrue();
        cacheControl.Should().Be("public, max-age=2592000");
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

    [IntegrationFact]
    public async Task Basic_DevAdmin_ShouldNotBeInterceptedByFrontFallback()
    {
        //Act
        var (html, status) = await RenderRequestPageEx("/dev/settings");

        //Assert — отдаётся админка (_AdminHost), а не фронт
        status.Should().NotBe(StatusCodes.Status404NotFound);
        html.Should().NotContain("Front not found");
        html.Should().Contain("<base href=\"/dev/\" />");
    }

    [IntegrationFact]
    public async Task Maintenance_Disabled_FrontWorks()
    {
        //Arrange
        var optionService = AppFixture.ServiceProvider.GetRequiredService<IOptionService>();
        optionService.GetOption<MaintenanceModeOption>().Enable.Should().BeFalse();

        //Act
        var render = await RenderRequestPage("/");

        //Assert
        render.Should().Contain("Hello, world! from appTheme!");
    }

    [IntegrationFact]
    public async Task Maintenance_Enabled_FrontRenderClosed()
    {
        await WithMaintenanceEnabled(async client =>
        {
            //Act
            var res = await client.Request("/").AllowAnyHttpStatus().GetAsync();
            var html = await res.GetStringAsync();

            //Assert
            res.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
            html.Should().Contain("Сайт отключен");
        });
    }

    [IntegrationFact]
    public async Task Maintenance_Enabled_StaticAssetsStillServed()
    {
        await WithMaintenanceEnabled(async client =>
        {
            //Act
            var res = await client.Request("1.txt").AllowAnyHttpStatus().GetAsync();

            //Assert — css/js/img и прочие ассеты фронта нужны самой странице обслуживания
            res.StatusCode.Should().Be(StatusCodes.Status200OK);
            (await res.GetStringAsync()).Trim().Should().Be("1");
        });
    }

    [IntegrationFact]
    public async Task Maintenance_Enabled_HtmlStaticPageClosed()
    {
        await WithMaintenanceEnabled(async client =>
        {
            //Act — html-страницы в wwwroot (например SPA-шелл index.html) тоже закрываются
            var res = await client.Request("page.html").AllowAnyHttpStatus().GetAsync();

            //Assert
            res.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        });
    }

    [IntegrationFact]
    public async Task Maintenance_Enabled_PageRenderApiStillWorks_ByDefault()
    {
        await WithMaintenanceEnabled(async client =>
        {
            //Act — сайт закрыт, но рендер по API продолжает работать (мобильные приложения)
            var res = await client.Request("/api/PageRender/by-url")
                .AppendQueryParam("url", System.Web.HttpUtility.UrlEncode("/"))
                .AllowAnyHttpStatus().GetAsync();
            var body = await res.GetStringAsync();

            //Assert
            res.StatusCode.Should().Be(StatusCodes.Status200OK);
            body.Should().Contain("Hello, world! from appTheme!");
        });
    }

    [IntegrationFact]
    public async Task Maintenance_EnabledWithApiFlag_PageRenderApiClosed()
    {
        await WithMaintenanceEnabled(async client =>
        {
            //Act
            var res = await client.Request("/api/PageRender/by-url")
                .AppendQueryParam("url", System.Web.HttpUtility.UrlEncode("/"))
                .AllowAnyHttpStatus().GetAsync();

            //Assert
            res.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        }, enableForApiRender: true);
    }

    [IntegrationFact]
    public async Task Maintenance_Enabled_AdminPanelWorks()
    {
        await WithMaintenanceEnabled(async client =>
        {
            //Act
            var res = await client.Request("/dev/settings").AllowAnyHttpStatus().GetAsync();
            var html = await res.GetStringAsync();

            //Assert — админка продолжает работать
            res.StatusCode.Should().NotBe(StatusCodes.Status503ServiceUnavailable);
            html.Should().Contain("<base href=\"/dev/\" />");
        });
    }

    [IntegrationFact]
    public async Task Maintenance_FrontPageSource_RendersSpecifiedPage()
    {
        //Arrange
        var optionService = AppFixture.ServiceProvider.GetRequiredService<IOptionService>();
        var option = optionService.GetOption<MaintenanceModeOption>();
        var original = (option.Enable, option.MaintenancePageSource, option.RenderPageUrl);
        option.Enable = true;
        option.MaintenancePageSource = EMaintenancePageSource.FrontPage;
        option.RenderPageUrl = "/second";
        optionService.SaveOption(option);

        try
        {
            var client = AppFixture.GetClient();

            //Act — все запросы фронта отдают указанную страницу фронта
            var res = await client.Request("/").AllowAnyHttpStatus().GetAsync();
            var html = await res.GetStringAsync();

            //Assert
            res.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
            html.Should().Contain("SecondPage");
        }
        finally
        {
            (option.Enable, option.MaintenancePageSource, option.RenderPageUrl) = original;
            optionService.SaveOption(option);
        }
    }

    async Task WithMaintenanceEnabled(Func<IFlurlClient, Task> action, bool enableForApiRender = false)
    {
        var optionService = AppFixture.ServiceProvider.GetRequiredService<IOptionService>();
        var option = optionService.GetOption<MaintenanceModeOption>();
        var original = (option.Enable, option.EnableForApiRender);
        option.Enable = true;
        option.EnableForApiRender = enableForApiRender;
        optionService.SaveOption(option);

        try
        {
            await action(AppFixture.GetClient());
        }
        finally
        {
            (option.Enable, option.EnableForApiRender) = original;
            optionService.SaveOption(option);
        }
    }

    [IntegrationFact]
    public async Task Render_ImmediatelyFresh_AfterFrontFilesServiceWrite()
    {
        //Arrange
        var filesService = AppFixture.ServiceProvider.GetRequiredService<IFrontFilesService>();
        var marker = "ai_write_" + Guid.NewGuid().ToString("N");
        var original = filesService.ReadFile("default", "pages/index_page.hbs").Content;

        try
        {
            //Act — запись серверным сервисом (тот же путь, что у ИИ-инструментов фазы 6);
            //рендер должен отдавать новое содержимое сразу, не полагаясь на FileSystemWatcher
            filesService.SaveFile("default", "pages/index_page.hbs", original + "\n<h2>" + marker + "</h2>");
            var render = await RenderRequestPage("/");

            //Assert
            render.Should().Contain(marker, "после записи файла фронта рендер должен сразу отдавать новое содержимое");
        }
        finally
        {
            filesService.SaveFile("default", "pages/index_page.hbs", original);
        }
    }
}
