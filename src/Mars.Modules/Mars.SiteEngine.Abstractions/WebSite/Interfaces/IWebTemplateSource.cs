using Mars.SiteEngine.Abstractions.WebSite.Models;

namespace Mars.SiteEngine.Abstractions.WebSite.Interfaces;

public interface IWebTemplateSource
{
    public bool IsFileSystem { get; }
    public IEnumerable<WebPartSource> ReadParts();
}