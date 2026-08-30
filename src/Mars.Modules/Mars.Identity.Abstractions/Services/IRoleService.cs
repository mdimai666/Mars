using Mars.Contracts.Common;
using Mars.Identity.Abstractions.Dto.Roles;

namespace Mars.Identity.Abstractions.Services;

public interface IRoleService
{
    Task<RoleDetail?> Get(Guid id, CancellationToken cancellationToken);
    Task<RoleDetail> Create(CreateRoleQuery query, CancellationToken cancellationToken);
    Task<RoleDetail> Update(UpdateRoleQuery query, CancellationToken cancellationToken);
    Task<ListDataResult<RoleSummary>> List(ListRoleQuery query, CancellationToken cancellationToken);
    Task<PagingResult<RoleSummary>> ListTable(ListRoleQuery query, CancellationToken cancellationToken);
    Task<UserActionResult> Delete(Guid id, CancellationToken cancellationToken);
}
