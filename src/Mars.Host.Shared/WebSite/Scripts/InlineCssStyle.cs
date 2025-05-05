namespace Mars.Host.Shared.WebSite.Scripts;

public class InlineCssStyle : IWebSiteInjectContentPart
{
    public string Content { get; }
    public bool PlaceInHead { get; }
    public bool Scoped { get; }

    public InlineCssStyle(string content, bool placeInHead = false, bool scoped = false)
    {
        Content = content;
        PlaceInHead = placeInHead;
        Scoped = scoped;
    }

    public string HtmlContent() => Content;
}
