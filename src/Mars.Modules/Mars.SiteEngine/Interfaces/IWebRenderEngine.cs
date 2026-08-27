using Mars.Host.Shared.WebSite.Models;

namespace Mars.WebSiteProcessor.Interfaces;

public interface IWebRenderEngine
{
    void Setup();

    string RenderPage(RenderEngineRenderRequestContext renderContext, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}
