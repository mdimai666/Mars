using Flurl;
using Flurl.Http;
using Mars.Contracts.Common;
using Mars.Contracts.Renders;
using Mars.WebApiClient.Interfaces;

namespace Mars.WebApiClient.Implements;

internal class PageRenderServiceClient : BasicServiceClient, IPageRenderServiceClient
{
    public PageRenderServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "PageRender";
    }

    public Task<RenderActionResult<PostRenderResponse>> Render(Guid id, string? frontSlug = null)
        => _client.Request($"{_basePath}{_controllerName}/by-id", id)
                    .AppendQueryParam("frontSlug", frontSlug)
                    .GetJsonAsync<RenderActionResult<PostRenderResponse>>();
    public Task<RenderActionResult<PostRenderResponse>> RenderPost(string type, string slug, string? frontSlug = null)
        => _client.Request($"{_basePath}{_controllerName}/by-post/{type}/{slug}")
                    .AppendQueryParam("frontSlug", frontSlug)
                    .GetJsonAsync<RenderActionResult<PostRenderResponse>>();
    public Task<RenderActionResult<PostRenderResponse>> Render(string slug, string? frontSlug = null)
        => _client.Request($"{_basePath}{_controllerName}/by-slug", slug)
                    .AppendQueryParam("frontSlug", frontSlug)
                    .GetJsonAsync<RenderActionResult<PostRenderResponse>>();
    public Task<RenderActionResult<PostRenderResponse>> RenderUrl(string url, string? frontSlug = null)
    {
        ArgumentNullException.ThrowIfNull(url, nameof(url));
        if (!url.StartsWith('/')) throw new ArgumentException("url must start with '/'(slash)");
        return _client.Request($"{_basePath}{_controllerName}", "by-url")
                    .AppendQueryParam("url", url)
                    .AppendQueryParam("frontSlug", frontSlug)
                    .GetJsonAsync<RenderActionResult<PostRenderResponse>>();
    }

}
