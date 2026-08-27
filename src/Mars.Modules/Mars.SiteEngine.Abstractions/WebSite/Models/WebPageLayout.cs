using Mars.Shared.Contracts.WebSite.Models;

namespace Mars.Host.Shared.WebSite.Models;

public class WebPageLayout : WebSitePart
{
    public WebPageLayout(WebSitePart part) : base(part)
    {
        Type = WebSitePartType.Layout;
    }

}
