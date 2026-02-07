using Flurl.Http;
using Mars.Shared.Common;
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

    public Task DeleteMany(Guid[] ids)
        => _client.Request($"{_basePath}{_controllerName}/DeleteMany")
                    .AppendQueryParam(new { ids })
                    .OnError(OnStatus404ThrowException)
                    .DeleteAsync();

    public Task<UserTypeEditViewModel> GetEditModel(Guid id)
        => _client.Request($"{_basePath}{_controllerName}/edit", id)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<UserTypeEditViewModel>();

    public Task<ListDataResult<UserTypeListItemResponse>> List(ListUserTypeQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/list/offset")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<ListDataResult<UserTypeListItemResponse>>();

    public Task<PagingResult<UserTypeListItemResponse>> ListTable(TableUserTypeQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/list/page")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<PagingResult<UserTypeListItemResponse>>();

}
