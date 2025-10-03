namespace Mars.Host.Shared.WebSite.Scripts;

public interface IWebSiteInjectContentPart
{
    bool PlaceInHead { get; }
    float Order { get; }
    string HtmlContent();
}
