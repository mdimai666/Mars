using Flurl.Http;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Renders;
using Mars.WebApiClient.Interfaces;

namespace Mars.WebApiClient.Implements;

internal class PageRenderServiceClient : BasicServiceClient, IPageRenderServiceClient
{
    public PageRenderServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "PageRender";
    }

    public Task<RenderActionResult<PostRenderResponse>> Render(Guid id)
        => _client.Request($"{_basePath}{_controllerName}/by-id", id)
                    .GetJsonAsync<RenderActionResult<PostRenderResponse>>();
    public Task<RenderActionResult<PostRenderResponse>> RenderPost(string type, string slug)
        => _client.Request($"{_basePath}{_controllerName}/by-post/{type}/{slug}")
                    .GetJsonAsync<RenderActionResult<PostRenderResponse>>();
    public Task<RenderActionResult<PostRenderResponse>> Render(string slug)
        => _client.Request($"{_basePath}{_controllerName}/by-slug", slug)
                    .GetJsonAsync<RenderActionResult<PostRenderResponse>>();
    public Task<RenderActionResult<PostRenderResponse>> RenderUrl(string url)
    {
        ArgumentNullException.ThrowIfNull(url, nameof(url));
        if (!url.StartsWith('/')) throw new ArgumentException("url must start with '/'(slash)");
        return _client.Request($"{_basePath}{_controllerName}", "by-url")
                    .AppendQueryParam("url", url)
                    .GetJsonAsync<RenderActionResult<PostRenderResponse>>();
    }
}
