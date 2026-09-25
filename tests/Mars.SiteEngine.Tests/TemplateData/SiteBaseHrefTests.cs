using FluentAssertions;
using Mars.Core.Models;
using Mars.Server.Abstractions.Models;
using Mars.Server.Contracts.Options;
using Mars.SiteEngine.Abstractions.Models;
using Mars.SiteEngine.Abstractions.TemplateData;
using Mars.SiteEngine.Abstractions.WebSite.Models;
using Mars.SiteEngine.Contracts.Options;
using Mars.SiteEngine.Contracts.WebSite.Models;
using Mars.SiteEngine.Handlebars;
using Mars.SiteEngine.Scriban;
using Mars.SiteEngine.Scriban.Extensions;
using Mars.Test.Common.Constants;
using Mars.TemplateEngine.Providers.ScribanProvider;
using NSubstitute;

namespace Mars.SiteEngine.Tests.TemplateData;

public class SiteBaseHrefTests
{
    [Theory]
    [InlineData(null, "/")]
    [InlineData("", "/")]
    [InlineData("/sbn", "/sbn/")]
    [InlineData("/sbn/", "/sbn/")]
    [InlineData("/app2/deep", "/app2/deep/")]
    public void FromFrontUrl_Normalizes(string? frontUrl, string expected)
    {
        SiteBaseHref.FromFrontUrl(frontUrl).Should().Be(expected);
    }

    [Theory]
    [InlineData(null, "/")]
    [InlineData("/sbn", "/sbn/")]
    public void HandlebarsEngine_FillsSiteBaseVariable(string? mountUrl, string expected)
    {
        var renderContext = BuildRenderContext("<base href=\"{{site_base}}\" />", mountUrl);
        var engine = new HandlebarsWebRenderEngine(null, SiteHandlebarsTestFactory.CreateFactory(), renderContext.AppFront);

        engine.RenderPage(renderContext, null!, default).Trim()
            .Should().Be($"<base href=\"{expected}\" />");
    }

    [Theory]
    [InlineData(null, "/")]
    [InlineData("/sbn", "/sbn/")]
    public void ScribanEngine_FillsSiteBaseVariable(string? mountUrl, string expected)
    {
        var renderContext = BuildRenderContext("<base href=\"{{ site_base }}\" />", mountUrl);
        var engine = new ScribanWebRenderEngine(null,
            new ScribanEngineFactory([new SiteScribanFunctionsContributor()]), renderContext.AppFront);

        engine.RenderPage(renderContext, Substitute.For<IServiceProvider>(), default).Trim()
            .Should().Be($"<base href=\"{expected}\" />");
    }

    static RenderEngineRenderRequestContext BuildRenderContext(string pageContent, string? mountUrl)
    {
        var sys = new SiteSettings { SiteUrl = "http://localhost" };
        var webClientRequest = new WebClientRequest(new Uri(sys.SiteUrl));

        var rootPart = new WebSitePart(WebSitePartType.Root, "_root", "_root", "", "@Body", new Dictionary<string, string>(), "Root");
        var pagePart = new WebSitePart(WebSitePartType.Page, "index", "index", "", pageContent,
            new Dictionary<string, string> { ["page"] = "/" }, "Index");
        var template = new WebSiteTemplate([rootPart, pagePart]);

        var af = new MarsAppFront
        {
            Configuration = new AppFrontSettingsCfg { Path = "" },
            Features = new(),
            Front = mountUrl is null ? null : new FrontItem { Slug = "t", Url = mountUrl },
        };

        var ctx = new PageRenderContext
        {
            Request = webClientRequest,
            SiteSettings = sys,
            User = new RenderContextUser(UserConstants.AuthorizedUserInfo),
            IsDevelopment = true,
            TemplateContextVariables = [],
            RenderParam = new RenderParam { AllowLayout = false },
        };

        return new RenderEngineRenderRequestContext(webClientRequest, af, template, template.IndexPage, ctx, ctx.RenderParam);
    }
}
