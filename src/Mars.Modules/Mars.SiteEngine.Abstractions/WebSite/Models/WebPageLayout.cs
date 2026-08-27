using Mars.Contracts.WebSite.Models;

namespace Mars.SiteEngine.Abstractions.WebSite.Models;

public class WebPageLayout : WebSitePart
{
    public WebPageLayout(WebSitePart part) : base(part)
    {
        Type = WebSitePartType.Layout;
    }

}
