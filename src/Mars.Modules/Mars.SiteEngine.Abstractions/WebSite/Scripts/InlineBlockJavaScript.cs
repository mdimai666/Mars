namespace Mars.Host.Shared.WebSite.Scripts;

public class InlineBlockJavaScript : IWebSiteInjectContentPart
{
    public string Content { get; }
    public bool PlaceInHead { get; }
    public float Order { get; }

    public string TypeAttribute = "text/javascript";
    public string? NameAttr { get; }

    public InlineBlockJavaScript(string content, bool placeInHead = false, float order = 10f)
    {
        Content = content;
        PlaceInHead = placeInHead;
        Order = order;
    }

    string attrs => $"type=\"{TypeAttribute}\" {(string.IsNullOrEmpty(NameAttr) ? "" : $"name=\"{NameAttr}\"")}";

    public string HtmlContent() => $"<script {attrs}>{Content}</script>";
}
