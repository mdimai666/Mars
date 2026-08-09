using Mars.Shared.Common;
using Mars.Shared.Contracts.Renders;

namespace Mars.WebApiClient.Interfaces;

public interface IPageRenderServiceClient
{
    Task<RenderActionResult<PostRenderResponse>> Render(Guid id, string? frontSlug = null);
    Task<RenderActionResult<PostRenderResponse>> RenderPost(string type, string slug, string? frontSlug = null);
    Task<RenderActionResult<PostRenderResponse>> Render(string slug, string? frontSlug = null);
    Task<RenderActionResult<PostRenderResponse>> RenderUrl(string url, string? frontSlug = null);

}
