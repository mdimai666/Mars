using System.Text.Json;
using FluentAssertions;
using Mars.Core.Models;
using Mars.Server.Abstractions.Models;
using Mars.Server.Contracts.Options;
using Mars.Server.Contracts.ViewModels;
using Mars.SiteEngine.Abstractions.Models;
using Mars.SiteEngine.Abstractions.WebSite.Models;
using Mars.SiteEngine.Contracts.WebSite.Models;
using Mars.SiteEngine.Handlebars;
using Mars.SiteEngine.Handlebars.TemplateData;
using Mars.SiteEngine.Host.Endpoints;
using Mars.SiteEngine.Host.Services;
using Mars.Test.Common.Constants;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Mars.SiteEngine.Tests.RenderEngineRenders;

public class RenderEngineRenderTests
{
    [Fact]
    public void Nameof_RenderPipelineTypes_Compiles()
    {
        _ = nameof(HandlebarsWebRenderEngine);
        _ = nameof(MapWebSiteProcessor);
        _ = nameof(WebSiteRequestProcessor);
        //_ = nameof(PrepareHostHtml.PreparePageContext);
        //_ = nameof(ViewModelController.InitialSiteDataViewModel);
        _ = nameof(InitialSiteDataViewModel);
        //_ = nameof(PageRenderContextOld);
        _ = nameof(PageRenderContext);

        //_ = nameof(RenderEngineRenderRequestContext);

        /*
        1. Request + url
        2. RenderContext
        3. Render Engine
        4. Page source, render param
        5. Parse #context & functions
        6. compile
        7. renderResponse
        */
    }

    WebSiteTemplate GetTemplateByPage(string content, string title)
    {
        var pageAttr = new Dictionary<string, string>();
        var rootAttr = new Dictionary<string, string>();
        WebPage index = new(new WebSitePart(WebSitePartType.Page, "index", "index.hbs", "", content, pageAttr, title), "/", title);
        WebRoot root = new(new WebSitePart(WebSitePartType.Root, "_root", "_root.hbs", "", "@Body", rootAttr, "Root"));

        return new WebSiteTemplate(new Dictionary<string, WebRoot>() { ["/"] = root }, [index], index);
    }

    RenderEngineRenderRequestContext GetRenderContext(string content, object? data = null)
    {
        var sys = new SiteSettings { SiteUrl = "http://localhost" };
        var dataDict = JsonSerializer.SerializeToNode(data).Deserialize<Dictionary<string, object?>>();
        var user = UserConstants.AuthorizedUserInfo;

        var template = GetTemplateByPage(content, "Index page");
        var httpContext = Substitute.For<HttpContext>();
        var webClientRequest = new WebClientRequest(new Uri(sys.SiteUrl));
        var af = new MarsAppFront
        {
            Configuration = new AppFrontSettingsCfg()
            {
                Path = "",
            },
            Features = new()
        };
        var ctx = new PageRenderContext()
        {
            Request = webClientRequest,
            SiteSettings = sys,
            User = new RenderContextUser(user),
            IsDevelopment = true,
            TemplateContextVaribles = dataDict ?? [],
            RenderParam = new RenderParam()
        };
        var renderParam = new RenderParam() { AllowLayout = false };
        return new RenderEngineRenderRequestContext(webClientRequest, af, template, template.IndexPage, ctx, renderParam);
    }

    [Fact]
    public void Render_SimpleTemplateRender_Succeeds()
    {
        // Arrange
        var data = new
        {
            ok = true,
        };
        var content = @"{{#if ok}}OK{{else}}NO{{/if}}";

        var context = GetRenderContext(content, data);
        var renderEngine = new HandlebarsWebRenderEngine(null, context.AppFront);

        // Act
        var html = renderEngine.RenderPage(context, null!, default);

        // Assert
        html.Trim().Should().Be("OK");
    }

    [Fact]
    public void Render_ContextHaveBasicData_HasData()
    {
        // Arrange
        var content = @"{{_user.FullName}}|{{_req.Host}}|{{SiteSettings.SiteUrl}}";
        var context = GetRenderContext(content);
        var renderEngine = new HandlebarsWebRenderEngine(null, context.AppFront);

        _ = nameof(HandlebarsTmpCtxBasicDataContext);
        var dict = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            ["_user.FullName"] = context.PageContext.User.FullName,
            ["_req.Host"] = context.PageContext.Request.Host.ToString(),
            ["SiteSettings.SiteUrl"] = context.PageContext.SiteSettings.SiteUrl,
        };

        // Act
        var html = renderEngine.RenderPage(context, null!, default);

        // Assert
        var renderData = html.Trim().Split('|', StringSplitOptions.TrimEntries);
        for (int i = 0; i < renderData.Length; i++)
        {
            var expect = dict.Values.ElementAt(i);
            var val = renderData[i];
            val.Should().Be(expect);
        }
    }

}
