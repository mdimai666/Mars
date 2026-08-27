namespace Mars.Host.Shared.WebSite.Scripts;

public class BackendHostScripts
{
    public string[] ExtraMetaTags { get; init; } = [];
    public string[] HeaderStyles { get; init; } = [];
    public string[] HeaderScripts { get; init; } = [];
    public string[] FooterScripts { get; init; } = [];
    public string? BlazorScripts { get; init; }
    public string[] InlineHeaderStyles { get; init; } = [];
    public string[] InlineHeaderScripts { get; init; } = [];
    public string[] InlineFooterScripts { get; init; } = [];

    public string BuildHtml(string content, string htmlStart, string middle, string htmlEnd)
    {
        //return $$"""
        //{{ExtraMetaTags}}
        //{{HeaderStyles}}
        //{{InlineHeaderStyles}}
        //{{HeaderScripts}}
        //{{InlineHeaderScripts}}
        //{{content}}
        //{{FooterScripts}}
        //{{BlazorScripts}}
        //{{InlineFooterScripts}}
        //""";

        return string.Join('\n', [
            htmlStart,
            ..ExtraMetaTags,
            ..HeaderStyles,
            ..InlineHeaderStyles,
            ..HeaderScripts,
            ..InlineHeaderScripts,
            middle,
            "\r\n",
            content,
            "\r\n",
            ..FooterScripts,
            BlazorScripts,
            ..InlineFooterScripts,
            htmlEnd
        ]);
    }

    public static (string allForHead, string allForFooter) Prepare(IEnumerable<IWebSiteInjectContentPart> parts)
    {
        return (
            string.Join('\n', parts.Where(s => s.PlaceInHead).Select(s => s.HtmlContent())),
            string.Join('\n', parts.Where(s => !s.PlaceInHead).Select(s => s.HtmlContent()))
            );
    }
}

