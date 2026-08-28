using Mars.Core.Exceptions;
using Mars.Contracts.Common;
using Mars.Identity.Contracts.Roles;
using Mars.Identity.Contracts.ViewModels;

namespace Mars.WebApiClient.Interfaces;

public interface IRoleServiceClient
{
    Task<RoleDetailResponse?> Get(Guid id);

    /// <summary>
    /// Создает
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="MarsValidationException"></exception>
    /// <exception cref="RoleActionException"></exception>
    Task<RoleDetailResponse> Create(CreateRoleRequest request);
    Task<RoleDetailResponse> Update(UpdateRoleRequest request);
    Task<ListDataResult<RoleSummaryResponse>> List(ListRoleQueryRequest filter);
    Task<PagingResult<RoleSummaryResponse>> ListTable(TableRoleQueryRequest filter);
    Task Delete(Guid id);
    Task<EditRolesViewModelDto> EditRolesViewModel();
    Task<UserActionResult> SaveRoleClaims(EditRolesViewModelDto dto);

}
