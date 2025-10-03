namespace Mars.Host.Shared.WebSite.Scripts;

public class InlineBlockJavaScript : IWebSiteInjectContentPart
{
    public string Content { get; }
    public bool PlaceInHead { get; }
    public float Order { get; }

    public InlineBlockJavaScript(string content, bool placeInHead = false, float order = 10f)
    {
        Content = content;
        PlaceInHead = placeInHead;
        Order = order;
    }

    public string HtmlContent() => Content;
}
