using Mars.Shared.Common;
using Mars.Shared.Contracts.Renders;

namespace Mars.WebApiClient.Interfaces;

public interface IPageRenderServiceClient
{
    Task<RenderActionResult<PostRenderResponse>> Render(Guid id);
    Task<RenderActionResult<PostRenderResponse>> RenderPost(string type, string slug);
    Task<RenderActionResult<PostRenderResponse>> Render(string slug);
    Task<RenderActionResult<PostRenderResponse>> RenderUrl(string url);

}
