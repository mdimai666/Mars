using Mars.SiteEngine.Abstractions.WebSite.Models;

namespace Mars.SiteEngine.Interfaces;

public interface IWebRenderEngine
{
    void Setup();

    string RenderPage(RenderEngineRenderRequestContext renderContext, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}
