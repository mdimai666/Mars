using FluentAssertions;
using Mars.AppFrontEngines.Integration.Tests.Common;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite.Interfaces;
using Mars.Host.Shared.WebSite.Models;
using Mars.Integration.Tests.Attributes;
using Mars.Test.Common.Constants;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebSiteProcessor.Handlebars.TemplateData;
using Mars.WebSiteProcessor.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Mars.AppFrontEngines.Integration.Tests.HandlebarsEngine;

public class WebSiteTemplateTests : BaseAppFrontTests<HandlebarsAppFrontApplicationFixture>
{
    private readonly IWebTemplateService _webTemplateService;

    public WebSiteTemplateTests(HandlebarsAppFrontApplicationFixture appFixture) : base(appFixture)
    {

        _fixture.Customize(new FixtureCustomize());
        _ = nameof(WebFilesReadFilesystemService);
        _ = nameof(WebTemplateService.ScanSite);
        var app = AppFixture.ServiceProvider.GetRequiredService<IMarsAppProvider>();
        _webTemplateService = Substitute.For<IWebTemplateService>();
        app.FirstApp.Features.Set<IWebTemplateService>(_webTemplateService);
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
