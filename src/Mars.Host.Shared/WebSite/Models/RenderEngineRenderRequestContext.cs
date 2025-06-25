using Mars.Host.Shared.Models;

namespace Mars.Host.Shared.WebSite.Models;

public class RenderEngineRenderRequestContext
{
    public WebClientRequest Request { get; }
    public MarsAppFront AppFront { get; }
    public WebSiteTemplate WebSiteTemplate { get; }
    public WebPage Page { get; }
    public PageRenderContext PageContext { get; }
    public RenderParam RenderParam { get; }

    public RenderEngineRenderRequestContext(
        WebClientRequest request,
        MarsAppFront app,
        WebSiteTemplate webSiteTemplate,
        WebPage page,
        PageRenderContext pageContext,
        RenderParam renderParam)
    {
        Request = request;
        AppFront = app;
        WebSiteTemplate = webSiteTemplate;
        Page = page;
        PageContext = pageContext;
        RenderParam = renderParam;
    }

}
