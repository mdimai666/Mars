namespace Mars.Host.Shared.WebSite.Scripts;

public interface IWebSiteInjectContentPart
{
    public bool PlaceInHead { get; }
    public string HtmlContent();
}
