using Flurl.Http;
using Mars.Shared.Common;
using Mars.Shared.Contracts.PostJsons;
using Mars.Shared.Contracts.Posts;
using Mars.WebApiClient.Interfaces;

namespace Mars.WebApiClient.Implements;

internal class PostJsonServiceClient : BasicServiceClient, IPostJsonServiceClient
{
    public PostJsonServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "PostJson";
    }

    public Task<PostJsonResponse?> Get(Guid id, bool renderContent = true)
        => _client.Request($"{_basePath}{_controllerName}", id)
                    .AppendQueryParam("renderContent", renderContent)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<PostJsonResponse?>();

    public Task<PostJsonResponse?> GetBySlug(string slug, string type, bool renderContent = true)
        => _client.Request($"{_basePath}{_controllerName}/by-type/{type}/item/{slug}")
                    .AppendQueryParam("renderContent", renderContent)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<PostJsonResponse?>();

    public Task<ListDataResult<PostJsonResponse>> List(ListPostQueryRequest filter, string type)
        => _client.Request($"{_basePath}{_controllerName}/by-type/{type}/list/offset")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<ListDataResult<PostJsonResponse>>();

    public Task<PagingResult<PostJsonResponse>> ListTable(TablePostQueryRequest filter, string type)
        => _client.Request($"{_basePath}{_controllerName}/by-type/{type}/list/offset")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<PagingResult<PostJsonResponse>>();
    public Task<PostJsonResponse> Create(CreatePostJsonRequest request)
        => _client.Request($"{_basePath}{_controllerName}")
                    .PostJsonAsync(request)
                    .ReceiveJson<PostJsonResponse>();
    public Task<PostJsonResponse> Update(UpdatePostJsonRequest request)
        => _client.Request($"{_basePath}{_controllerName}")
                    .PutJsonAsync(request)
                    .ReceiveJson<PostJsonResponse>();
}
