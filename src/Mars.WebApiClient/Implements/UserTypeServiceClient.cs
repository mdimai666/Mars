using Flurl.Http;
using Mars.Shared.Common;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.UserTypes;
using Mars.WebApiClient.Interfaces;

namespace Mars.WebApiClient.Implements;

internal class UserTypeServiceClient : BasicServiceClient, IUserTypeServiceClient
{
    public UserTypeServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "UserType";
    }

    public Task<UserTypeDetailResponse?> Get(Guid id)
        => _client.Request($"{_basePath}{_controllerName}", id)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<UserTypeDetailResponse?>();

    public Task<UserTypeSummaryResponse> Create(CreateUserTypeRequest request)
        => _client.Request($"{_basePath}{_controllerName}")
                    .PostJsonAsync(request)
                    .ReceiveJson<UserTypeSummaryResponse>();

    public Task<UserTypeSummaryResponse> Update(UpdateUserTypeRequest request)
        => _client.Request($"{_basePath}{_controllerName}")
                    .PutJsonAsync(request)
                    .ReceiveJson<UserTypeSummaryResponse>();

    public Task Delete(Guid id)
        => _client.Request($"{_basePath}{_controllerName}", id)
                    .OnError(OnStatus404ThrowException)
                    .DeleteAsync();

    public Task<UserTypeEditViewModel> GetEditModel(Guid id)
        => _client.Request($"{_basePath}{_controllerName}/edit", id)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<UserTypeEditViewModel>();

    public Task<ListDataResult<UserTypeListItemResponse>> List(ListUserTypeQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<ListDataResult<UserTypeListItemResponse>>();

    public Task<PagingResult<UserTypeListItemResponse>> ListTable(TableUserTypeQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/ListTable")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<PagingResult<UserTypeListItemResponse>>();

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

}
