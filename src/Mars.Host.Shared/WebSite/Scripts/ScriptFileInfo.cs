using System.Text.RegularExpressions;

namespace Mars.Host.Shared.WebSite.Scripts;

public record ScriptFileInfo : IWebSiteInjectContentPart, IWebSiteExternalAssert
{
    public string ScriptHtml { get; }
    public bool PlaceInHead { get; }
    public Uri ScriptUrl { get; }
    public string? ScriptName { get; }
    public Version? ScriptVersion { get; }
    public float Order { get; }

    public ScriptInfoType Type { get; }

    //public Dictionary<string,string> Attributes { get; }

    public static Regex ExtractScriptUrl = new(@"(src|href)\s*=\s*""([^""]+)""");

    /// <summary>
    /// </summary>
    /// <param name="scriptTag">
    /// <list type="table">
    /// <item>&lt;script src="https://some_cdnjs.com/script.js?x=1"&gt;&lt;/script&gt;</item>
    /// <item>&lt;link rel="stylesheet" href="https://some_cdnjs.com"/&gt;</item>
    /// </list>
    /// </param>
    /// <param name="scriptName"></param>
    /// <param name="version"></param>
    /// <param name="order"></param>
    public ScriptFileInfo(string scriptTag, bool placeInHead = false, string? scriptName = null, string? version = null, float order = 10)
    {
        ScriptHtml = scriptTag;
        PlaceInHead = placeInHead;
        ScriptUrl = new Uri(ExtractScriptUrl.Match(scriptTag).Value);
        var ext = Path.GetExtension(ScriptUrl.LocalPath).TrimStart('.');
        Type = DetectType(ext);

        ScriptName = scriptName ?? Path.GetFileName(ScriptUrl.LocalPath);
        Order = order;
        ScriptVersion = version == null ? null : new Version(version);
    }

    public ScriptFileInfo(Uri url, bool placeInHead = false, string? scriptName = null, string? version = null, float order = 10)
    {
        ScriptUrl = url;
        PlaceInHead = placeInHead;
        var ext = Path.GetExtension(ScriptUrl.LocalPath).TrimStart('.');
        Type = DetectType(ext);

        ScriptName = scriptName ?? Path.GetFileName(ScriptUrl.LocalPath);
        Order = order;
        ScriptVersion = version == null ? null : new Version(version);

        ScriptHtml = BuildScriptTag();
    }

    ScriptInfoType DetectType(string fileExt)
    {
        return fileExt.ToLower() switch
        {
            "js" => ScriptInfoType.Script,
            "css" => ScriptInfoType.Style,
            _ => ScriptInfoType.Unknown,
        };
    }

    string BuildScriptTag()
    {
        return Type switch
        {
            ScriptInfoType.Style => $"<link rel=\"stylesheet\" href=\"{ScriptUrl}\">",
            ScriptInfoType.Script => $"<script src=\"{ScriptUrl}\"></script>",
            _ => $"<link href=\"{ScriptUrl}\">"
        };
    }

    public string HtmlContent() => ScriptHtml;
}
