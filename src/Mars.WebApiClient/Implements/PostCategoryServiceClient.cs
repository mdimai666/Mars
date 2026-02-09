using Flurl.Http;
using Mars.Shared.Common;
using Mars.Shared.Contracts.PostCategories;
using Mars.WebApiClient.Interfaces;

namespace Mars.WebApiClient.Implements;

internal class PostCategoryServiceClient : BasicServiceClient, IPostCategoryServiceClient
{
    public PostCategoryServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "PostCategory";
    }

    public Task<PostCategoryDetailResponse?> Get(Guid id)
        => _client.Request($"{_basePath}{_controllerName}", id)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<PostCategoryDetailResponse?>();

    public Task<PostCategoryDetailResponse?> GetBySlug(string slug, string type)
        => _client.Request($"{_basePath}{_controllerName}/by-type/{type}/item/{slug}")
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<PostCategoryDetailResponse?>();

    public Task<PostCategoryDetailResponse> Create(CreatePostCategoryRequest request)
        => _client.Request($"{_basePath}{_controllerName}")
                    .PostJsonAsync(request)
                    .ReceiveJson<PostCategoryDetailResponse>();

    public Task<PostCategoryDetailResponse> Update(UpdatePostCategoryRequest request)
        => _client.Request($"{_basePath}{_controllerName}")
                    .PutJsonAsync(request)
                    .ReceiveJson<PostCategoryDetailResponse>();

    public Task Delete(Guid id)
        => _client.Request($"{_basePath}{_controllerName}", id)
                    .OnError(OnStatus404ThrowException)
                    .DeleteAsync();

    public Task DeleteMany(Guid[] ids)
        => _client.Request($"{_basePath}{_controllerName}/DeleteMany")
                    .AppendQueryParam(new { ids })
                    .OnError(OnStatus404ThrowException)
                    .DeleteAsync();

    public Task<ListDataResult<PostCategoryListItemResponse>> List(ListPostCategoryQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/list/offset")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<ListDataResult<PostCategoryListItemResponse>>();

    public Task<PagingResult<PostCategoryListItemResponse>> ListTable(TablePostCategoryQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/list/page")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<PagingResult<PostCategoryListItemResponse>>();

    public Task<ListDataResult<PostCategoryListItemResponse>> List(string categoryType, ListPostCategoryQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/by-type/{categoryType}/list/offset")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<ListDataResult<PostCategoryListItemResponse>>();

    public Task<PagingResult<PostCategoryListItemResponse>> ListTable(string categoryType, TablePostCategoryQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/by-type/{categoryType}/list/page")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<PagingResult<PostCategoryListItemResponse>>();

    public Task<ListDataResult<PostCategoryListItemResponse>> ListForPostType(string postType, ListPostCategoryQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/for-post-type/{postType}/list/offset")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<ListDataResult<PostCategoryListItemResponse>>();

    public Task<PagingResult<PostCategoryListItemResponse>> ListTableForPostType(string postType, TablePostCategoryQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/for-post-type/{postType}/list/page")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<PagingResult<PostCategoryListItemResponse>>();

    public Task<PostCategoryEditViewModel> GetEditModel(Guid id)
        => _client.Request($"{_basePath}{_controllerName}/edit", id)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<PostCategoryEditViewModel>();

    public Task<PostCategoryEditViewModel> GetBlankModel(string categoryType, string postType)
        => _client.Request($"{_basePath}{_controllerName}/edit/blank", categoryType, postType)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<PostCategoryEditViewModel>();
}
