using System.Text.Json;
using Mars.Core.Models;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.WebSite.Models;
using Mars.Shared.Contracts.WebSite.Models;
using Mars.Shared.Options;
using Mars.Shared.ViewModels;
using Mars.Test.Common.Constants;
using Mars.WebSiteProcessor.Endpoints;
using Mars.WebSiteProcessor.Handlebars;
using Mars.WebSiteProcessor.Handlebars.TemplateData;
using Mars.WebSiteProcessor.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Test.Mars.WebSiteProcessor.RenderEngineRenders;

public class RenderEngineRenderTests
{
    [Fact]
    public void Test1()
    {
        _ = nameof(HandlebarsWebRenderEngine);
        _ = nameof(MapWebSiteProcessor);
        _ = nameof(WebSiteRequestProcessor);
        //_ = nameof(PrepareHostHtml.PreparePageContext);
        _ = nameof(PrepareHostHtml);
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
        WebPage index = new(new WebSitePart(WebSitePartType.Page, "index", "index.html", "", content, pageAttr, title), "/", title);
        WebRoot root = new WebRoot(new WebSitePart(WebSitePartType.Root, "_root", "_root.html", "", "@Body", rootAttr, "Root"));

        return new WebSiteTemplate(new Dictionary<string, WebRoot>() { ["/"] = root }, [index], index);
    }

    RenderEngineRenderRequestContext GetRenderContext(string content, object? data = null)
    {
        var sys = new SysOptions { SiteUrl = "http://localhost" };
        var dataDict = JsonSerializer.SerializeToNode(data).Deserialize<Dictionary<string, object>>();
        var user = UserConstants.TestUser;

        var template = GetTemplateByPage(content, "Index page");
        var httpContext = Substitute.For<HttpContext>();
        var webClientRequest = new WebClientRequest(new Uri(sys.SiteUrl));
        var af = new MarsAppFront
        {
            Configuration = new AppFrontSettingsCfg()
            {
                Mode = AppFrontMode.None,
                Path = "",
            },
            Features = new()
        };
        var ctx = new PageRenderContext()
        {
            Request = webClientRequest,
            SysOptions = sys,
            User = new RenderContextUser(user),
            IsDevelopment = true,
            TemplateContextVaribles = dataDict ?? new(),
            RenderParam = new RenderParam()
        };
        var renderParam = new RenderParam() { AllowLayout = false };
        return new RenderEngineRenderRequestContext(webClientRequest, af, template, template.IndexPage, ctx, renderParam);
    }

    [Fact]
    public void Render_SimpleTemplateRender_ShouldOk()
    {
        // Arrange
        var data = new
        {
            ok = true,
        };
        var content = @"{{#if ok}}OK{{else}}NO{{/if}}";

        var context = GetRenderContext(content, data);
        var renderEngine = new HandlebarsWebRenderEngine();

        // Act
        var html = renderEngine.RenderPage(context, null!, default);

        // Assert
        html.Trim().Should().Be("OK");
    }

    [Fact]
    public void Render_ContextHaveBasicData_ShouldHaveData()
    {
        // Arrange
        var content = @"{{_user.FullName}}|{{_req.Host}}|{{SysOptions.SiteUrl}}";
        var renderEngine = new HandlebarsWebRenderEngine();
        var context = GetRenderContext(content);

        _ = nameof(PrepareHostHtml);
        _ = nameof(HandlebarsTmpCtxBasicDataContext);
        var dict = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            ["_user.FullName"] = context.PageContext.User.FullName,
            ["_req.Host"] = context.PageContext.Request.Host.ToString(),
            ["SysOptions.SiteUrl"] = context.PageContext.SysOptions.SiteUrl,
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
