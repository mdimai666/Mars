using Mars.Host.Shared.Dto.Roles;
using Mars.Shared.Common;

namespace Mars.Host.Shared.Services;

public interface IRoleService
{
    Task<RoleDetail?> Get(Guid id, CancellationToken cancellationToken);
    Task<RoleDetail> Create(CreateRoleQuery query, CancellationToken cancellationToken);
    Task<RoleDetail> Update(UpdateRoleQuery query, CancellationToken cancellationToken);
    Task<ListDataResult<RoleSummary>> List(ListRoleQuery query, CancellationToken cancellationToken);
    Task<PagingResult<RoleSummary>> ListTable(ListRoleQuery query, CancellationToken cancellationToken);
    Task<UserActionResult> Delete(Guid id, CancellationToken cancellationToken);
}
