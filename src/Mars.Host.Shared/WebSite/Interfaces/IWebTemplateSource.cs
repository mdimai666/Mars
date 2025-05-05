using Mars.Host.Shared.WebSite.Models;

namespace Mars.Host.Shared.WebSite.Interfaces;

public interface IWebTemplateSource
{
    public bool IsFileSystem { get; }
    public IEnumerable<WebPartSource> ReadParts();
}