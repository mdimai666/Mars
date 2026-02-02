using Flurl.Http;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Users;
using Mars.WebApiClient.Interfaces;

namespace Mars.WebApiClient.Implements;

internal class UserServiceClient : BasicServiceClient, IUserServiceClient
{
    public UserServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "User";
    }

    public Task<UserDetailResponse?> Get(Guid id)
        => _client.Request($"{_basePath}{_controllerName}", id)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<UserDetailResponse?>();

    public Task<UserDetailResponse> Create(CreateUserRequest request)
        => _client.Request($"{_basePath}{_controllerName}")
                    .PostJsonAsync(request)
                    .ReceiveJson<UserDetailResponse>();

    public Task<UserDetailResponse> Update(UpdateUserRequest request)
        => _client.Request($"{_basePath}{_controllerName}")
                    .PutJsonAsync(request)
                    .ReceiveJson<UserDetailResponse>();

    public Task Delete(Guid id)
        => _client.Request($"{_basePath}{_controllerName}", id)
                    .OnError(OnStatus404ThrowException)
                    .DeleteAsync();

    public Task DeleteMany(Guid[] ids)
        => _client.Request($"{_basePath}{_controllerName}/DeleteMany")
                    .AppendQueryParam(new { ids })
                    .OnError(OnStatus404ThrowException)
                    .DeleteAsync();

    public Task<ListDataResult<UserListItemResponse>> List(ListUserQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/list/offset")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<ListDataResult<UserListItemResponse>>();

    public Task<ListDataResult<UserDetailResponse>> ListDetail(ListUserQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}", "/list/detail/offset")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<ListDataResult<UserDetailResponse>>();

    public Task<PagingResult<UserListItemResponse>> ListTable(TableUserQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/list/page")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<PagingResult<UserListItemResponse>>();

    public Task<PagingResult<UserDetailResponse>> ListTableDetail(TableUserQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/list/detail/page")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<PagingResult<UserDetailResponse>>();

    public Task<UserActionResult> UpdateUserRoles(Guid id, IEnumerable<Guid> roles)
    {
        throw new NotImplementedException();
    }
    public Task<UserActionResult> SetPassword(SetUserPasswordByIdRequest request)
        => _client.Request($"{_basePath}{_controllerName}/SetPassword")
                    .PutJsonAsync(request)
                    .ReceiveJson<UserActionResult>();
    public Task<UserActionResult> SendInvation(Guid id)
        => _client.Request($"{_basePath}{_controllerName}/SendInvation", id)
                    .PostJsonAsync(new { })
                    .ReceiveJson<UserActionResult>();

    public Task<UserEditViewModel> GetEditModel(Guid id)
        => _client.Request($"{_basePath}{_controllerName}/edit", id)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<UserEditViewModel>();

    public Task<UserEditViewModel> GetUserBlank(string type)
        => _client.Request($"{_basePath}{_controllerName}/edit/blank", type)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<UserEditViewModel>();
}
