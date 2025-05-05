using Mars.Core.Exceptions;
using Mars.Shared.Contracts.WebSite.Models;

namespace Mars.Host.Shared.WebSite.Models;

public class WebSiteTemplate
{
    public WebRoot RootPage { get; private init; }
    public IReadOnlyCollection<WebPage> Pages { get; private init; }
    public IReadOnlyCollection<WebSitePart> Parts { get; private init; }
    public IReadOnlyCollection<WebPageLayout> Layouts { get; private init; }
    public IDictionary<string, WebRoot> Roots { get; private init; }


    public WebPage? Page404 { get; init; }
    public WebPage? Page500 { get; init; }
    public WebPage IndexPage { get; init; }

    public Guid Hash { get; init; } = Guid.NewGuid();

    public WebSiteTemplate(
        IDictionary<string, WebRoot> roots,
        IReadOnlyCollection<WebPage> pages,
        WebPage indexPage,
        IReadOnlyCollection<WebPageLayout>? layouts = null,
        WebPage? page404 = null,
        WebPage? page500 = null)
    {
        Roots = roots;
        Pages = pages;
        Layouts = layouts ?? new List<WebPageLayout>();
        Parts = [.. Roots.Values, .. Pages, .. Layouts];

        RootPage = Roots.Values.FirstOrDefault(s => s.StartPath == "/") ?? throw new NotFoundException("web site Root file not found");
        IndexPage = indexPage;
        Page404 = page404;
        Page500 = page500;

    }

    public WebSiteTemplate(IEnumerable<WebSitePart> w_parts)
    {
        Parts = w_parts.ToList();
        Roots = Parts.Where(s => s.Type == WebSitePartType.Root).Select(s => new WebRoot(s)).ToDictionary(s => s.StartPath);
        Pages = Parts.Where(s => s.Type == WebSitePartType.Page).Select(s => new WebPage(s)).ToList();
        Layouts = Parts.Where(s => s.Type == WebSitePartType.Layout).Select(s => new WebPageLayout(s)).ToList();

        Page404 = Pages.FirstOrDefault(s => s.Url == "/404");
        Page500 = Pages.FirstOrDefault(s => s.Url == "/500");
        IndexPage = Pages.FirstOrDefault(s => s.Url == "/") ?? Pages.FirstOrDefault(s => s.Name == "index") ?? throw new NotFoundException("index page not found");
        RootPage = Roots.Values.FirstOrDefault(s => s.StartPath == "/") ?? throw new NotFoundException("web site Root file not found");

        CheckParts();
    }

    public WebSiteTemplate(IEnumerable<WebPartSource> w_parts) : this(w_parts.Select(WebSitePart.FromHandlebarsSource))
    {
    }

    void CheckParts()
    {
        if (RootPage is null) throw new FileNotFoundException("_root.(*ext) file");

        if (IndexPage is null) throw new FileNotFoundException("html file with attribute \"@page \"/\" \" not found");

        var requiredLayouts = Pages.Where(s => s.Attributes.ContainsKey("layout"))
            .Select(s => s.Attributes["layout"])
            .Where(s => s != "null")
            .Distinct();

        var existLayout = Layouts.Select(s => s.Name);

        var undefLayouts = requiredLayouts.Except(existLayout);

        if (undefLayouts.Count() > 0)
        {
            throw new FileNotFoundException(string.Join("; ", undefLayouts.Select(s => s + ".html")));
        }
    }
}
