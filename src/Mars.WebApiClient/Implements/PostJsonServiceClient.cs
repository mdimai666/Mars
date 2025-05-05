using Mars.Shared.Common;
using Mars.Shared.Contracts.Posts;
using Mars.WebApiClient.Interfaces;
using Flurl.Http;

namespace Mars.WebApiClient.Implements;

internal class PostJsonServiceClient : BasicServiceClient, IPostJsonServiceClient
{
    public PostJsonServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "PostJson";
    }

    public Task<PostJsonResponse?> Get(Guid id)
        => _client.Request($"{_basePath}{_controllerName}", id)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<PostJsonResponse?>();

    public Task<PostJsonResponse?> GetBySlug(string slug, string type)
        => _client.Request($"{_basePath}{_controllerName}", type, slug)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<PostJsonResponse?>();

    public Task<ListDataResult<PostJsonResponse>> List(ListPostQueryRequest filter, string type)
        => _client.Request($"{_basePath}{_controllerName}", type)
                    .AppendQueryParam(filter)
                    .GetJsonAsync<ListDataResult<PostJsonResponse>>();

    public Task<PagingResult<PostJsonResponse>> ListTable(TablePostQueryRequest filter, string type)
        => _client.Request($"{_basePath}{_controllerName}/ListTable", type)
                    .AppendQueryParam(filter)
                    .GetJsonAsync<PagingResult<PostJsonResponse>>();

}
