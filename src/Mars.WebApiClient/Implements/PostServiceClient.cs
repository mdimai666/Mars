using Flurl.Http;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Posts;
using Mars.WebApiClient.Interfaces;

namespace Mars.WebApiClient.Implements;

internal class PostServiceClient : BasicServiceClient, IPostServiceClient
{
    public PostServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "Post";
    }

    public Task<PostDetailResponse?> Get(Guid id, bool renderContent = true)
        => _client.Request($"{_basePath}{_controllerName}", id)
                    .AppendQueryParam("renderContent", renderContent)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<PostDetailResponse?>();

    public Task<PostDetailResponse?> GetBySlug(string slug, string type, bool renderContent = true)
        => _client.Request($"{_basePath}{_controllerName}/by-type/{type}/item/{slug}")
                    .AppendQueryParam("renderContent", renderContent)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<PostDetailResponse?>();

    public Task<PostDetailResponse> Create(CreatePostRequest request)
        => _client.Request($"{_basePath}{_controllerName}")
                    .PostJsonAsync(request)
                    .ReceiveJson<PostDetailResponse>();

    public Task<PostDetailResponse> Update(UpdatePostRequest request)
        => _client.Request($"{_basePath}{_controllerName}")
                    .PutJsonAsync(request)
                    .ReceiveJson<PostDetailResponse>();

    public Task Delete(Guid id)
        => _client.Request($"{_basePath}{_controllerName}", id)
                    .OnError(OnStatus404ThrowException)
                    .DeleteAsync();

    public Task DeleteMany(Guid[] ids)
        => _client.Request($"{_basePath}{_controllerName}/DeleteMany")
                    .AppendQueryParam(new { ids })
                    .OnError(OnStatus404ThrowException)
                    .DeleteAsync();

    public Task<ListDataResult<PostListItemResponse>> List(ListPostQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/list/offset")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<ListDataResult<PostListItemResponse>>();

    public Task<PagingResult<PostListItemResponse>> ListTable(TablePostQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/list/page")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<PagingResult<PostListItemResponse>>();

    public Task<ListDataResult<PostListItemResponse>> List(string postType, ListPostQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/by-type/{postType}/list/offset")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<ListDataResult<PostListItemResponse>>();

    public Task<PagingResult<PostListItemResponse>> ListTable(string postType, TablePostQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/by-type/{postType}/list/page")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<PagingResult<PostListItemResponse>>();

    public Task<PostEditViewModel> GetEditModel(Guid id)
        => _client.Request($"{_basePath}{_controllerName}/edit", id)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<PostEditViewModel>();

    public Task<PostEditViewModel> GetPostBlank(string type)
        => _client.Request($"{_basePath}{_controllerName}/edit/blank", type)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<PostEditViewModel>();
}
