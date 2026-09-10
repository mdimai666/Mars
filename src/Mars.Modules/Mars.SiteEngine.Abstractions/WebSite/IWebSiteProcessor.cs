using Mars.SiteEngine.Abstractions.WebSite.Models;
using Microsoft.AspNetCore.Http;

namespace Mars.SiteEngine.Abstractions.WebSite;

public interface IWebSiteProcessor
{
    public Task Response(HttpContext httpContext, CancellationToken cancellationToken);
    public Task<RenderResult> RenderRequest(HttpContext httpContext, RenderParam? param, CancellationToken cancellationToken);
    public Task<RenderResult> RenderPage(WebPage page, HttpContext httpContext, RenderParam? param, CancellationToken cancellationToken);
    public Task<RenderResult> RenderPage404(HttpContext httpContext, RenderParam? param, CancellationToken cancellationToken);
}
