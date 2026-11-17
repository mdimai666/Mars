using FluentAssertions;
using Flurl.Http;
using Mars.Integration.Tests.Attributes;
using Mars.SiteEngine.Abstractions.Services;
using Mars.SiteEngine.Integration.Tests.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.SiteEngine.Integration.Tests.HandlebarsEngine;

/// <summary>
/// Рендер страниц админ-фронта по URL из @page (/api/AdminFront/RenderByPath)
/// и шелл админки на адресах /dev/p/{path}.
/// </summary>
[Collection(HandlebarsAppFrontCollection.CollectionName)]
public class AdminFrontRenderByPathTests : BaseAppFrontTests<HandlebarsAppFrontApplicationFixture>
{
    public AdminFrontRenderByPathTests(HandlebarsAppFrontApplicationFixture appFixture) : base(appFixture)
    {
    }

    [IntegrationFact]
    public async Task DevP_AnyPath_ServesAdminShell_NotFrontFallback()
    {
        //Act
        var (html, status) = await RenderRequestPageEx("/dev/p/some-page");

        //Assert — отдаётся админка (_AdminHost), а не фронт
        status.Should().NotBe(StatusCodes.Status404NotFound);
        html.Should().NotContain("Front not found");
        html.Should().Contain("<base href=\"/dev/\" />");
    }

    [IntegrationFact]
    public async Task RenderByPath_EmptyPath_ReturnsIndexPage()
    {
        //Act
        var client = AppFixture.GetClient();
        var res = await client.Request("/api/AdminFront/RenderByPath").AllowAnyHttpStatus().GetAsync();

        //Assert — индексная страница админ-фронта (@page "/")
        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        (await res.GetStringAsync()).Should().NotBeNullOrWhiteSpace();
    }

    [IntegrationFact]
    public async Task RenderByPath_ExactUrl_ReturnsPageBody()
    {
        //Arrange
        var files = AppFixture.ServiceProvider.GetRequiredService<IFrontFilesService>();
        var marker = "devp_" + Guid.NewGuid().ToString("N");
        files.SaveFile("admin", "pages/devp_test.hbs", "@page \"/devp-test\"\n\n<div>" + marker + "</div>");

        try
        {
            //Act
            var client = AppFixture.GetClient();
            var res = await client.Request("/api/AdminFront/RenderByPath")
                .AppendQueryParam("path", "/devp-test")
                .AllowAnyHttpStatus().GetAsync();
            var html = await res.GetStringAsync();

            //Assert — тело страницы без директивы и без обвязки фронта
            res.StatusCode.Should().Be(StatusCodes.Status200OK);
            html.Should().Contain(marker);
            html.Should().NotContain("@page");
        }
        finally
        {
            files.Delete("admin", "pages/devp_test.hbs");
        }
    }

    [IntegrationFact]
    public async Task RenderByPath_ParamUrl_RouteVariableAvailableInTemplate()
    {
        //Arrange
        var files = AppFixture.ServiceProvider.GetRequiredService<IFrontFilesService>();
        files.SaveFile("admin", "pages/devp_item.hbs", "@page \"/devp-item/{slug}\"\n\n<div>item-{{slug}}</div>");

        try
        {
            //Act
            var client = AppFixture.GetClient();
            var res = await client.Request("/api/AdminFront/RenderByPath")
                .AppendQueryParam("path", "/devp-item/abc")
                .AllowAnyHttpStatus().GetAsync();
            var html = await res.GetStringAsync();

            //Assert
            res.StatusCode.Should().Be(StatusCodes.Status200OK);
            html.Should().Contain("item-abc");
        }
        finally
        {
            files.Delete("admin", "pages/devp_item.hbs");
        }
    }

    [IntegrationFact]
    public async Task RenderByPath_QueryString_AvailableAsReqQuery()
    {
        //Arrange
        var files = AppFixture.ServiceProvider.GetRequiredService<IFrontFilesService>();
        files.SaveFile("admin", "pages/devp_query.hbs", "@page \"/devp-query\"\n\n<div>q-{{_req.Query.marker}}</div>");

        try
        {
            //Act — queryString пробрасывается отдельным параметром (как это делает RemotePageViewer)
            var client = AppFixture.GetClient();
            var res = await client.Request("/api/AdminFront/RenderByPath")
                .AppendQueryParam("path", "/devp-query")
                .AppendQueryParam("queryString", "?marker=hello")
                .AllowAnyHttpStatus().GetAsync();
            var html = await res.GetStringAsync();

            //Assert
            res.StatusCode.Should().Be(StatusCodes.Status200OK);
            html.Should().Contain("q-hello");
        }
        finally
        {
            files.Delete("admin", "pages/devp_query.hbs");
        }
    }

    [IntegrationFact]
    public async Task RenderByPath_UnknownUrl_Returns404()
    {
        //Act
        var client = AppFixture.GetClient();
        var res = await client.Request("/api/AdminFront/RenderByPath")
            .AppendQueryParam("path", "/no-such-admin-front-page")
            .AllowAnyHttpStatus().GetAsync();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
}