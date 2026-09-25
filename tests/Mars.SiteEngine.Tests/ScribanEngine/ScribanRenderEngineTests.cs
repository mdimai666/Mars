using FluentAssertions;
using Mars.Core.Models;
using Mars.QueryLang.Services;
using Mars.Server.Abstractions.Interfaces;
using Mars.Server.Abstractions.Models;
using Mars.Server.Contracts.Options;
using Mars.SiteEngine.Abstractions.Models;
using Mars.SiteEngine.Abstractions.WebSite.Models;
using Mars.SiteEngine.Contracts.WebSite.Models;
using Mars.SiteEngine.Scriban;
using Mars.SiteEngine.Scriban.Extensions;
using Mars.TemplateEngine.Providers.ScribanProvider;
using Mars.Test.Common.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using NSubstitute;

namespace Mars.SiteEngine.Tests.ScribanEngine;

public class ScribanRenderEngineTests
{
    readonly IServiceProvider _serviceProvider;

    public ScribanRenderEngineTests()
    {
        _serviceProvider = Substitute.For<IServiceProvider>();
    }

    ScribanWebRenderEngine CreateEngine(MarsAppFront appFront) =>
        new(null, new ScribanEngineFactory([new SiteScribanFunctionsContributor()]), appFront);

    static WebSiteTemplate BuildTemplate(string pageContent, string? layoutName = null, params WebSitePart[] extraParts)
    {
        var pageAttrs = new Dictionary<string, string> { ["page"] = "/" };
        if (layoutName is not null) pageAttrs["layout"] = layoutName;

        var parts = new List<WebSitePart>
        {
            new WebSitePart(WebSitePartType.Root, "_root", "_root.sbn", "", "@Body", new Dictionary<string, string>(), "Root"),
            new WebSitePart(WebSitePartType.Page, "index", "index.sbn", "", pageContent, pageAttrs, "Index page"),
        };
        parts.AddRange(extraParts);

        return new WebSiteTemplate(parts);
    }

    string Render(WebSiteTemplate template, Dictionary<string, object?>? extraVars = null)
    {
        var sys = new SiteSettings { SiteUrl = "http://localhost" };
        var user = UserConstants.AuthorizedUserInfo;

        var httpContext = Substitute.For<HttpContext>();
        var webClientRequest = new WebClientRequest(new Uri(sys.SiteUrl));
        var af = new MarsAppFront
        {
            Configuration = new AppFrontSettingsCfg { Path = "" },
            Features = new(),
        };

        var ctx = new PageRenderContext
        {
            Request = webClientRequest,
            SiteSettings = sys,
            User = new RenderContextUser(user),
            IsDevelopment = true,
            TemplateContextVariables = [],
            RenderParam = new RenderParam(),
        };

        if (extraVars is not null)
        {
            foreach (var (key, val) in extraVars)
            {
                ctx.TemplateContextVariables[key] = val;
            }
        }

        var renderContext = new RenderEngineRenderRequestContext(webClientRequest, af, template, template.IndexPage, ctx, ctx.RenderParam);

        var engine = CreateEngine(af);
        return engine.RenderPage(renderContext, _serviceProvider, default).Trim();
    }

    [Fact]
    public void Render_NativeIfExpression_Works()
    {
        var template = BuildTemplate("{{ if true }}OK{{ else }}NO{{ end }}");

        Render(template).Should().Be("OK");
    }

    [Fact]
    public void Render_BasicContextVariables_HaveUserData()
    {
        var template = BuildTemplate("{{ _user.FullName }}|{{ SiteSettings.SiteUrl }}");

        var html = Render(template);

        html.Should().Contain(UserConstants.AuthorizedUserInfo.FullName);
        html.Should().Contain("http://localhost");
    }

    [Fact]
    public void Render_MobileVariable_IsAvailable()
    {
        var template = BuildTemplate("{{ if mobile }}M{{ else }}D{{ end }}");

        Render(template).Should().Be("D");
    }

    [Fact]
    public void Render_LayoutWrapsPage_ViaBodyVariable()
    {
        var layout = new WebSitePart(WebSitePartType.Layout, "main_layout", "main_layout.sbn", "",
            "<div>{{ body }}</div>", new Dictionary<string, string>(), "Layout");
        var template = BuildTemplate("PAGE", layoutName: "main_layout", layout);

        Render(template).Should().Be("<div>PAGE</div>");
    }

    [Fact]
    public void Render_IncludeBlock_FromParts()
    {
        var block = new WebSitePart(WebSitePartType.Block, "block1", "block1.sbn", "",
            "[B]", new Dictionary<string, string>(), null);
        var template = BuildTemplate("X{{ include 'block1' }}Y", extraParts: block);

        Render(template).Should().Be("X[B]Y");
    }

    [Fact]
    public void Render_ErrorsVariable_IsAccessible()
    {
        // $-префиксные сайт-переменные в Scriban доступны без $ (в Scriban $name — локальная переменная);
        // список — встроенный list-аксессор: размер через .size, не .Count
        var template = BuildTemplate("{{ errors.size }}");

        Render(template).Should().Be("0");
    }

    [Fact]
    public void Render_SiteBaseConcat_BuildsFrontRelativeUrl()
    {
        var template = BuildTemplate("{{ url = site_base + 'posts' }}{{ url }}");

        Render(template).Should().Be("/posts");
    }

    [Fact]
    public void ContextFunction_AddsQueryResultsToTemplate()
    {
        var queryLangProcessing = Substitute.For<IQueryLangProcessing>();
        queryLangProcessing.Process(Arg.Any<PageRenderContext>(), Arg.Any<IReadOnlyCollection<KeyValuePair<string, string>>>(), Arg.Any<Dictionary<string, object>?>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object?> { ["greeting"] = "hello" });
        _serviceProvider.GetService(typeof(IQueryLangProcessing)).Returns(queryLangProcessing);

        var template = BuildTemplate("""{{ context "x = 1" }}{{ greeting }}""");

        Render(template).Should().Be("hello");
        queryLangProcessing.Received(1).Process(
            Arg.Any<PageRenderContext>(),
            Arg.Is<IReadOnlyCollection<KeyValuePair<string, string>>>(q => q.Any(s => s.Key == "x" && s.Value == "1")),
            Arg.Any<Dictionary<string, object>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ContextFunction_MultilineBody_ParsesAllLines()
    {
        var queryLangProcessing = Substitute.For<IQueryLangProcessing>();
        queryLangProcessing.Process(Arg.Any<PageRenderContext>(), Arg.Any<IReadOnlyCollection<KeyValuePair<string, string>>>(), Arg.Any<Dictionary<string, object>?>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object?> { ["greeting"] = "hello" });
        _serviceProvider.GetService(typeof(IQueryLangProcessing)).Returns(queryLangProcessing);

        var template = BuildTemplate("""
            {{ context '
            a == 1
            b = ef.post.Take(2)
            ' }}{{ greeting }}
            """);

        Render(template).Should().Be("hello");
        queryLangProcessing.Received(1).Process(
            Arg.Any<PageRenderContext>(),
            Arg.Is<IReadOnlyCollection<KeyValuePair<string, string>>>(q =>
                q.Count == 2
                && q.Any(s => s.Key == "a")
                && q.Any(s => s.Key == "b" && s.Value == "ef.post.Take(2)")),
            Arg.Any<Dictionary<string, object>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void HelpFunction_ListsRegisteredFunctions()
    {
        Render(BuildTemplate("{{ help }}")).Should().Contain("context").And.Contain("site_head");
    }

    [Fact]
    public void DictionaryVariables_IterateAsKeyValue()
    {
        var vars = new Dictionary<string, object?>
        {
            ["items"] = new Dictionary<int, string> { [1] = "page=1", [2] = "page=2" },
        };

        Render(BuildTemplate("{{ for kv in items }}[{{ kv.Key }}={{ kv.Value }}]{{ end }}"), vars)
            .Should().Be("[1=page=1][2=page=2]");
    }

    [Fact]
    public void LFunction_LocalizesByKey()
    {
        var stringLocalizer = Substitute.For<IStringLocalizer>();
        stringLocalizer["Title"].Returns(new LocalizedString("Title", "Заголовок"));
        var appFrontLocalizer = Substitute.For<IAppFrontLocalizer>();
        appFrontLocalizer.GetLocalizer(Arg.Any<string>()).Returns(stringLocalizer);
        _serviceProvider.GetService(typeof(IAppFrontLocalizer)).Returns(appFrontLocalizer);

        var template = BuildTemplate("""{{ L "Title" }}""");

        Render(template).Should().Be("Заголовок");
    }

    [Fact]
    public void IffFunction_EvaluatesExpression()
    {
        var template = BuildTemplate("""{{ if iff "2 > 1" }}+{{ else }}-{{ end }}""");

        Render(template).Should().Be("+");
    }

    [Fact]
    public void TextHelpers_Work()
    {
        Render(BuildTemplate("""{{ text_ellipsis "12345" 2 }}""")).Should().Be("12...");
        Render(BuildTemplate("""{{ striphtml "<div>123</div>" }}""")).Should().Be("123");
        Render(BuildTemplate("""{{ nl2br "a\nb" }}""")).Should().Be("a<br>b");
        Render(BuildTemplate("""{{ to_humanized_size 1536 }}""")).Should().NotBeEmpty();
    }

    [Fact]
    public void RawBlock_OutputsPartSource()
    {
        var block = new WebSitePart(WebSitePartType.Block, "block1", "block1.sbn", "",
            "{{ x }}", new Dictionary<string, string>(), null);
        var template = BuildTemplate("""{{ raw_block "block1" }}""", extraParts: block);

        Render(template).Should().Be("{{ x }}");
    }
}
