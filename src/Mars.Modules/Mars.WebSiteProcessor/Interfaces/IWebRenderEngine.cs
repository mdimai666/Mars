using Mars.Host.Shared.Models;
using Mars.Host.Shared.WebSite.Models;
using Microsoft.AspNetCore.Builder;

namespace Mars.WebSiteProcessor.Interfaces;

public interface IWebRenderEngine
{
    void Setup();
    void UseFront(IApplicationBuilder app);
    string RenderPage(RenderEngineRenderRequestContext renderContext, IServiceProvider serviceProvider, CancellationToken cancellationToken);

    string RenderPage(MarsAppFront MarsAppFront, PageRenderContext ctx,
        WebRoot root, WebPage? page, IReadOnlyCollection<WebSitePart>? parts,
        IServiceProvider serviceProvider, CancellationToken cancellationToken);

}
