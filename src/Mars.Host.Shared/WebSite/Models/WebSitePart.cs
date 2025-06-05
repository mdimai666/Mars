using System.Diagnostics;
using Mars.Shared.Contracts.WebSite.Models;

namespace Mars.Host.Shared.WebSite.Models;

[DebuggerDisplay("[{Type}] {FileName} / {Title}")]
public class WebSitePart
{
    public WebSitePartType Type { get; init; }
    public string FileName { get; init; }
    public string Name { get; init; }

    /// <summary>
    /// relative path
    /// </summary>
    public string FileRelPath { get; init; }
    public string FileFullPath { get; init; }
    public string Content { get; init; }
    public string? Title { get; init; }

    public IReadOnlyDictionary<string, string> Attributes { get; init; }

    //public bool Disable { get; set; }

    public WebSitePart(WebSitePartType type, string name, string fileRelPath, string fileFullPath, string content, IReadOnlyDictionary<string, string> attributes, string? title)
    {
        Type = type;
        FileName = Path.GetFileName(fileRelPath);
        Name = name;
        FileRelPath = fileRelPath;
        FileFullPath = fileFullPath;
        Content = content;
        Attributes = attributes;
        Title = title;
    }

    protected WebSitePart(WebSitePart part) : this(
        type: part.Type,
        name: part.Name,
        fileRelPath: part.FileRelPath,
        fileFullPath: part.FileFullPath,
        content: part.Content,
        attributes: part.Attributes,
        title: part.Title)
    {
    }

    /// <summary>
    /// parse from handlebars source
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static WebSitePart FromHandlebarsSource(WebPartSource source)
    {
        var (attr, content) = ParseContent(source.Content);

        var type = WebSitePartType.Block;

        if (Path.GetFileNameWithoutExtension(source.Name).Equals("_root", StringComparison.OrdinalIgnoreCase))
        {
            type = WebSitePartType.Root;
        }
        else if (attr.ContainsKey("page"))
        {
            type = WebSitePartType.Page;
        }
        //@inherits LayoutComponentBase
        else if (attr.TryGetValue("inherits", out var inherits) && inherits == "LayoutComponentBase")
        {
            type = WebSitePartType.Layout;
        }

        return new WebSitePart(
            type: type,
            name: source.Name,
            fileRelPath: source.SourceRelLink,
            fileFullPath: source.SourceFullLink,
            content: content,
            attributes: attr,
            title: source.Title);
    }

    public static WebSitePart Copy(WebSitePart part) => new(part);

    public static (Dictionary<string, string> attributes, string content) ParseContent(string htmlContent)
    {
        Dictionary<string, string> attrs = new();
        int stopIndex = 0;

        using (StringReader sr = new StringReader(htmlContent))
        {
            string? line;
            while ((line = sr.ReadLine()) != null)
            {
                //stopIndex += line.Length - 1;
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.StartsWith('@') && line.Length > 3)
                {
                    var sp = line.Trim().Split(' ', 2);
                    string key = sp[0].TrimStart('@');
                    string val = "";
                    if (sp.Length > 1)//TODO: сделать по уму
                    {
                        val = sp[1].Trim().Trim('"', '\'');
                    }
                    if (attrs.ContainsKey(key))
                    {
                        attrs[key] = val;
                    }
                    else
                    {
                        attrs.Add(key, val);
                    }
                }
                else if (line.StartsWith("{{!--"))
                {
                    continue;
                }
                else
                {
                    stopIndex = htmlContent.IndexOf(line);
                    break;
                }
            }
            //stopIndex = sr.
        }

        var content = htmlContent.Substring(stopIndex, htmlContent.Length - stopIndex);

        return (attrs, content);
    }
}
