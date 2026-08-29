using FluentAssertions;
using Mars.AppFrontEngines.Integration.Tests.Common;
using Mars.Integration.Tests.Attributes;
using Mars.SiteEngine.Abstractions.Models;
using Mars.SiteEngine.Abstractions.WebSite;
using Mars.SiteEngine.Abstractions.WebSite.Interfaces;
using Mars.SiteEngine.Abstractions.WebSite.Models;
using Mars.SiteEngine.Handlebars.TemplateData;
using Mars.SiteEngine.Host.Services;
using Mars.Test.Common.Constants;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Mars.AppFrontEngines.Integration.Tests.HandlebarsEngine;

[Collection(HandlebarsAppFrontCollection.CollectionName)]
public class WebSiteTemplateTests : BaseAppFrontTests<HandlebarsAppFrontApplicationFixture>, IDisposable
{
    private readonly IWebTemplateService _webTemplateService;
    private readonly MarsAppFront _app;
    private readonly IWebTemplateService? _originalTemplateService;

    public WebSiteTemplateTests(HandlebarsAppFrontApplicationFixture appFixture) : base(appFixture)
    {

        _fixture.Customize(new FixtureCustomize());
        _ = nameof(WebFilesReadFilesystemService);
        _ = nameof(WebTemplateService.ScanSite);
        _app = AppFixture.ServiceProvider.GetRequiredService<IWebRenderEngineLocator>().GetAppFrontForUrl("/")!;
        _originalTemplateService = _app.Features.Get<IWebTemplateService>();
        _webTemplateService = Substitute.For<IWebTemplateService>();
        _app.Features.Set<IWebTemplateService>(_webTemplateService);

        // Движок кэширует скомпилированные шаблоны в IMemoryCache на 30 минут по page.Url:
        // страницы реальной темы, отрендеренные предыдущими тестами, иначе подменят результат мока
        var memoryCache = AppFixture.ServiceProvider.GetRequiredService<IMemoryCache>();
        foreach (var url in new[] { "/", "/second", "/404" })
            foreach (var onlyBody in new[] { 0, 1 })
                foreach (var allowLayout in new[] { 0, 1 })
                    memoryCache.Remove($"HandlebarsWebRenderEngine::::AppCacheKey[{url},{onlyBody},{allowLayout}]");
    }

    public void Dispose()
    {
        // мок подменяет сервис на общем (кэшированном) MarsAppFront — без восстановления
        // следующие тесты сьюта рендерили бы мок вместо реальной темы
        if (_originalTemplateService is not null)
            _app.Features.Set(_originalTemplateService);
    }

    WebSiteTemplate EmptyWebSiteTemplate(string indexContent, WebPartSource[]? parts = null) =>
        new([
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
        var render = await RenderRequestPage("/");

        //Assert
        render.Should().Contain(UserConstants.TestUserFirstName);
    }

    [IntegrationFact]
    public async Task Basic_Page404_ShouldStatusCode404()
    {
        //Arrange
        var template = "index_page";
        // URL обязан быть /404 — по нему WebSiteTemplate определяет Page404
        var page404 = new WebPartSource("""
            @page /404

            <h1>page_404_mock</h1>
            """, "404", "404", "", "");
        _webTemplateService.Template.Returns(EmptyWebSiteTemplate(template, [page404]));

        //Act
        var (render, statusCode) = await RenderRequestPageEx("/non_exist_pageUrl_for_404");

        //Assert
        render.Should().Contain("page_404_mock");
        render.Should().NotContain("index_page");
        statusCode.Should().Be(StatusCodes.Status404NotFound);
    }
}
