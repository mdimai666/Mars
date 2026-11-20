using Mars.Contracts.Common;
using Mars.Identity.Abstractions.Dto.Roles;
using Mars.Identity.Contracts.ViewModels;

namespace Mars.Identity.Abstractions.Services;

public interface IRoleService
{
    Task<RoleDetail?> Get(Guid id, CancellationToken cancellationToken);
    Task<RoleDetail> Create(CreateRoleQuery query, CancellationToken cancellationToken);
    Task<RoleDetail> Update(UpdateRoleQuery query, CancellationToken cancellationToken);
    Task<ListDataResult<RoleSummary>> List(ListRoleQuery query, CancellationToken cancellationToken);
    Task<PagingResult<RoleSummary>> ListTable(ListRoleQuery query, CancellationToken cancellationToken);
    Task<UserActionResult> Delete(Guid id, CancellationToken cancellationToken);

    /// <summary>Матрица «все роли × все права каталога» с отметками выданных прав.</summary>
    Task<EditRolesViewModelDto> EditRolesViewModel();

    /// <summary>Дифф-замена прав ролей из матрицы; неизвестные каталогу ids прав игнорируются.</summary>
    Task<UserActionResult> SaveRoleClaims(EditRolesViewModelDto dto, CancellationToken cancellationToken);

    /// <summary>Есть ли право у текущего пользователя (через его роли и кэш прав ролей).</summary>
    Task<bool> HasCap(string cap);
}
