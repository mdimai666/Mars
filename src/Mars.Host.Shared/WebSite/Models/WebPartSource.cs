using System.Diagnostics;

namespace Mars.Host.Shared.WebSite.Models;

[DebuggerDisplay("WebPartSource={Name}/{Title}")]
public class WebPartSource
{
    public string Content { get; init; }
    public string Name { get; init; }
    public string Title { get; init; }
    public string FileName { get; init; }
    public string SourceFullLink { get; init; }
    public string SourceRelLink { get; init; }

    public WebPartSource(string content, string name, string title, string sourceFullLink, string sourceRelLink)
    {
        Content = content;
        Name = name;
        Title = title;
        FileName = Path.GetFileName(sourceFullLink);
        SourceFullLink = sourceFullLink;
        SourceRelLink = sourceRelLink;
    }
}
