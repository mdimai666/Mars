using Mars.SiteEngine.Contracts.Options;

namespace Mars.SiteEngine.Handlers;

public interface IFaviconGeneratorHandler
{
    /// <summary>
    /// Generate favicons icon files, manifest and return HTML meta tags
    /// </summary>
    /// <param name="faviconOption"></param>
    /// <param name="appName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>html meta tags</returns>
    Task<string> Handle(FaviconOption faviconOption, string appName, CancellationToken cancellationToken);
}
