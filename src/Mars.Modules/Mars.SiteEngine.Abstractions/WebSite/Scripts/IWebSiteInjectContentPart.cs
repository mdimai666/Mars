namespace Mars.SiteEngine.Abstractions.WebSite.Scripts;

public interface IWebSiteInjectContentPart
{
    bool PlaceInHead { get; }
    float Order { get; }
    string HtmlContent();
}
