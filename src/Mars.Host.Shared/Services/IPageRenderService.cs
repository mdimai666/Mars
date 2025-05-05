using Mars.Host.Shared.Dto.Renders;
using Mars.Host.Shared.WebSite.Models;
using Mars.Shared.Common;
using Microsoft.AspNetCore.Http;

namespace Mars.Host.Shared.Services;

public interface IPageRenderService
{
    Task<RenderActionResult<PostRenderDto>> RenderPage(WebPage page, HttpContext httpContext, CancellationToken cancellationToken);
    Task<RenderActionResult<PostRenderDto>> RenderPage404(HttpContext httpContext, CancellationToken cancellationToken);
    Task<RenderActionResult<PostRenderDto>> RenderUrl(PathString url, HttpContext httpContext, RenderParam? renderParam = null, CancellationToken cancellationToken = default);
    Task<RenderActionResult<PostRenderDto>> RenderPageBySlug(string slug, HttpContext httpContext, CancellationToken cancellationToken);
    Task<RenderActionResult<PostRenderDto>> RenderPostBySlug(string typeName, string slug, HttpContext httpContext, CancellationToken cancellationToken);
    Task<RenderActionResult<PostRenderDto>> RenderPostById(Guid postId, HttpContext httpContext, CancellationToken cancellationToken);

}
