using Mars.Shared.Common;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.PostTypes;
using Mars.WebApiClient.Interfaces;
using Flurl.Http;

namespace Mars.WebApiClient.Implements;

internal class PostTypeServiceClient : BasicServiceClient, IPostTypeServiceClient
{
    public PostTypeServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "PostType";
    }

    public Task<PostTypeDetailResponse?> Get(Guid id)
        => _client.Request($"{_basePath}{_controllerName}", id)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<PostTypeDetailResponse?>();

    public Task<PostTypeSummaryResponse> Create(CreatePostTypeRequest request)
        => _client.Request($"{_basePath}{_controllerName}")
                    .PostJsonAsync(request)
                    .ReceiveJson<PostTypeSummaryResponse>();

    public Task<PostTypeSummaryResponse> Update(UpdatePostTypeRequest request)
        => _client.Request($"{_basePath}{_controllerName}")
                    .PutJsonAsync(request)
                    .ReceiveJson<PostTypeSummaryResponse>();

    public Task Delete(Guid id)
        => _client.Request($"{_basePath}{_controllerName}", id)
                    .OnError(OnStatus404ThrowException)
                    .DeleteAsync();

    public Task DeleteMany(Guid[] ids)
        => _client.Request($"{_basePath}{_controllerName}/DeleteMany")
                    .AppendQueryParam(new { ids })
                    .OnError(OnStatus404ThrowException)
                    .DeleteAsync();

    public Task<PostTypeEditViewModel> GetEditModel(Guid id)
        => _client.Request($"{_basePath}{_controllerName}/edit", id)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<PostTypeEditViewModel>();

    public Task<ListDataResult<PostTypeListItemResponse>> List(ListPostTypeQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<ListDataResult<PostTypeListItemResponse>>();

    public Task<PagingResult<PostTypeListItemResponse>> ListTable(TablePostTypeQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/ListTable")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<PagingResult<PostTypeListItemResponse>>();

    public Task<UserActionResult> PostTypeImport(string json)
        => _client.Request($"{_basePath}{_controllerName}/PostTypeImport/")
                    .PostJsonAsync(json)
                    .ReceiveJson<UserActionResult>();

    public Task<UserActionResult> PostTypeImport(string json, string asPostType)
        => _client.Request($"{_basePath}{_controllerName}/PostTypeImport/")
                    .SetQueryParams(new { asPostType })
                    .PostJsonAsync(json)
                    .ReceiveJson<UserActionResult>();

    public Task<IReadOnlyCollection<MetaRelationModelResponse>> AllMetaRelationsStructure()
        => _client.Request($"{_basePath}{_controllerName}/AllMetaRelationsStructure")
                    .GetJsonAsync<IReadOnlyCollection<MetaRelationModelResponse>>();

    public Task<ListDataResult<MetaValueRelationModelSummaryResponse>> ListMetaValueRelationModels(MetaValueRelationModelsListQueryRequest request)
        => _client.Request($"{_basePath}{_controllerName}/ListMetaValueRelationModels")
                    .AppendQueryParam(request)
                    .GetJsonAsync<ListDataResult<MetaValueRelationModelSummaryResponse>>();
    public Task<IReadOnlyDictionary<Guid, MetaValueRelationModelSummaryResponse>> GetMetaValueRelationModels(string modelName, Guid[] ids)
        => _client.Request($"{_basePath}{_controllerName}/GetMetaValueRelationModels", modelName)
                    .SetQueryParams(new { ids })
                    .GetJsonAsync<IReadOnlyDictionary<Guid, MetaValueRelationModelSummaryResponse>>();

    public Task<PostTypePresentationEditViewModel> GetPresentationEditModel(Guid id)
        => _client.Request($"{_basePath}{_controllerName}/presentation/edit", id)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<PostTypePresentationEditViewModel>();

    public Task UpdatePresentation(UpdatePostTypePresentationRequest request)
        => _client.Request($"{_basePath}{_controllerName}/presentation/update")
                    .PutJsonAsync(request);
}
