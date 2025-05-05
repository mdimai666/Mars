using System.Web;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Renders;
using Mars.WebApiClient.Interfaces;
using Flurl.Http;

namespace Mars.WebApiClient.Implements;

internal class PageRenderServiceClient : BasicServiceClient, IPageRenderServiceClient
{
    public PageRenderServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "PageRender";
    }

    public Task<RenderActionResult<PostRenderResponse>> Render(Guid id)
        => _client.Request($"{_basePath}{_controllerName}", "Render", id)
                    .GetJsonAsync<RenderActionResult<PostRenderResponse>>();
    public Task<RenderActionResult<PostRenderResponse>> RenderPost(string type, string slug)
        => _client.Request($"{_basePath}{_controllerName}", "RenderPost", type, slug)
                    .GetJsonAsync<RenderActionResult<PostRenderResponse>>();
    public Task<RenderActionResult<PostRenderResponse>> Render(string slug)
        => _client.Request($"{_basePath}{_controllerName}", "Render", slug)
                    .GetJsonAsync<RenderActionResult<PostRenderResponse>>();
    public Task<RenderActionResult<PostRenderResponse>> RenderUrl(string url)
    {
        ArgumentNullException.ThrowIfNull(url, nameof(url));
        if (!url.StartsWith('/')) throw new ArgumentException("url must start with '/'(slash)");
        return _client.Request($"{_basePath}{_controllerName}", "RenderUrl", HttpUtility.UrlEncode(url))
                    .GetJsonAsync<RenderActionResult<PostRenderResponse>>();
    }
}
