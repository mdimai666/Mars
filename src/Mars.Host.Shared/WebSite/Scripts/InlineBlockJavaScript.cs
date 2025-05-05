namespace Mars.Host.Shared.WebSite.Scripts;

public class InlineBlockJavaScript : IWebSiteInjectContentPart
{
    public string Content { get; }
    public bool PlaceInHead { get; }

    public InlineBlockJavaScript(string content, bool placeInHead = false)
    {
        Content = content;
        PlaceInHead = placeInHead;
    }

    public string HtmlContent() => Content;
}
