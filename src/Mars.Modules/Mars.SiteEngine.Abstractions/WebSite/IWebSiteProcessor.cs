using Mars.SiteEngine.Abstractions.WebSite.Models;
using Microsoft.AspNetCore.Http;

namespace Mars.SiteEngine.Abstractions.WebSite;

public interface IWebSiteProcessor
{
    public Task Response(HttpContext httpContext, CancellationToken cancellationToken);
    public Task<RenderInfo> RenderRequest(HttpContext httpContext, RenderParam? param, CancellationToken cancellationToken);
    public Task<RenderInfo> RenderPage(WebPage page, HttpContext httpContext, RenderParam? param, CancellationToken cancellationToken);
    public Task<RenderInfo> RenderPage404(HttpContext httpContext, RenderParam? param, CancellationToken cancellationToken);
}
