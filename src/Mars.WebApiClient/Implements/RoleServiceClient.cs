using Mars.Shared.Common;
using Mars.Shared.Contracts.Roles;
using Mars.Shared.ViewModels;
using Mars.WebApiClient.Interfaces;
using Flurl.Http;

namespace Mars.WebApiClient.Implements;

internal class RoleServiceClient : BasicServiceClient, IRoleServiceClient
{
    public RoleServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "Role";
    }

    public Task<RoleDetailResponse?> Get(Guid id)
        => _client.Request($"{_basePath}{_controllerName}", id)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<RoleDetailResponse?>();

    public Task<RoleDetailResponse> Create(CreateRoleRequest request)
        => _client.Request($"{_basePath}{_controllerName}")
                    .PostJsonAsync(request)
                    .ReceiveJson<RoleDetailResponse>();

    public Task<RoleDetailResponse> Update(UpdateRoleRequest request)
        => _client.Request($"{_basePath}{_controllerName}")
                    .PutJsonAsync(request)
                    .ReceiveJson<RoleDetailResponse>();

    public Task Delete(Guid id)
        => _client.Request($"{_basePath}{_controllerName}", id)
                    .OnError(OnStatus404ThrowException)
                    .DeleteAsync();

    public Task<ListDataResult<RoleSummaryResponse>> List(ListRoleQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<ListDataResult<RoleSummaryResponse>>();

    public Task<PagingResult<RoleSummaryResponse>> ListTable(TableRoleQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/ListTable")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<PagingResult<RoleSummaryResponse>>();

    public Task<EditRolesViewModelDto> EditRolesViewModel()
    {
        throw new NotImplementedException();
    }

    public Task<UserActionResult> SaveRoleClaims(EditRolesViewModelDto dto)
    {
        throw new NotImplementedException();
    }
}
