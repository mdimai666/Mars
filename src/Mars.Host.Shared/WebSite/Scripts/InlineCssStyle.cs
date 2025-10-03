namespace Mars.Host.Shared.WebSite.Scripts;

public class InlineCssStyle : IWebSiteInjectContentPart
{
    public string Content { get; }
    public bool PlaceInHead { get; }
    public float Order { get; }
    public bool Scoped { get; }

    public InlineCssStyle(string content, bool placeInHead = false, float order = 10f, bool scoped = false)
    {
        Content = content;
        PlaceInHead = placeInHead;
        Order = order;
        Scoped = scoped;
    }

    public string HtmlContent() => Content;
}
