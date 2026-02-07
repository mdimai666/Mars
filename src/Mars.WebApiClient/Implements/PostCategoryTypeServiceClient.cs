using Flurl.Http;
using Mars.Shared.Common;
using Mars.Shared.Contracts.PostCategoryTypes;
using Mars.WebApiClient.Interfaces;

namespace Mars.WebApiClient.Implements;

internal class PostCategoryTypeServiceClient : BasicServiceClient, IPostCategoryTypeServiceClient
{
    public PostCategoryTypeServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "PostCategoryType";
    }

    public Task<PostCategoryTypeDetailResponse?> Get(Guid id)
        => _client.Request($"{_basePath}{_controllerName}", id)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<PostCategoryTypeDetailResponse?>();

    public Task<PostCategoryTypeDetailResponse?> GetByName(string typeName)
        => _client.Request($"{_basePath}{_controllerName}/by-name/{typeName}")
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<PostCategoryTypeDetailResponse?>();

    public Task<PostCategoryTypeSummaryResponse> Create(CreatePostCategoryTypeRequest request)
        => _client.Request($"{_basePath}{_controllerName}")
                    .PostJsonAsync(request)
                    .ReceiveJson<PostCategoryTypeSummaryResponse>();

    public Task<PostCategoryTypeSummaryResponse> Update(UpdatePostCategoryTypeRequest request)
        => _client.Request($"{_basePath}{_controllerName}")
                    .PutJsonAsync(request)
                    .ReceiveJson<PostCategoryTypeSummaryResponse>();

    public Task Delete(Guid id)
        => _client.Request($"{_basePath}{_controllerName}", id)
                    .OnError(OnStatus404ThrowException)
                    .DeleteAsync();

    public Task DeleteMany(Guid[] ids)
        => _client.Request($"{_basePath}{_controllerName}/DeleteMany")
                    .AppendQueryParam(new { ids })
                    .OnError(OnStatus404ThrowException)
                    .DeleteAsync();

    public Task<PostCategoryTypeEditViewModel> GetEditModel(Guid id)
        => _client.Request($"{_basePath}{_controllerName}/edit", id)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<PostCategoryTypeEditViewModel>();

    public Task<ListDataResult<PostCategoryTypeListItemResponse>> List(ListPostCategoryTypeQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/list/offset")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<ListDataResult<PostCategoryTypeListItemResponse>>();

    public Task<PagingResult<PostCategoryTypeListItemResponse>> ListTable(TablePostCategoryTypeQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/list/page")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<PagingResult<PostCategoryTypeListItemResponse>>();

}
