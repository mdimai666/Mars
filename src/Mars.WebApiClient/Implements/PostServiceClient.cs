using Mars.Shared.Common;
using Mars.Shared.Contracts.Posts;
using Mars.WebApiClient.Interfaces;
using Flurl.Http;

namespace Mars.WebApiClient.Implements;

internal class PostServiceClient : BasicServiceClient, IPostServiceClient
{
    public PostServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "Post";
    }

    public Task<PostDetailResponse?> Get(Guid id)
        => _client.Request($"{_basePath}{_controllerName}", id)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<PostDetailResponse?>();

    public Task<PostDetailResponse?> GetBySlug(string slug, string type)
        => _client.Request($"{_basePath}{_controllerName}/p", type, slug)
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

    public Task<ListDataResult<PostListItemResponse>> List(ListPostQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<ListDataResult<PostListItemResponse>>();

    public Task<PagingResult<PostListItemResponse>> ListTable(TablePostQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/ListTable")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<PagingResult<PostListItemResponse>>();

    public Task<ListDataResult<PostListItemResponse>> List(string postType, ListPostQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}", postType)
                    .AppendQueryParam(filter)
                    .GetJsonAsync<ListDataResult<PostListItemResponse>>();

    public Task<PagingResult<PostListItemResponse>> ListTable(string postType, TablePostQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/ListTable", postType)
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
