using Mars.Contracts.Common;
using Mars.Identity.Abstractions.Dto.Roles;

namespace Mars.Identity.Abstractions.Repositories;

public interface IRoleRepository : IDisposable
{
    Task<RoleDetail?> Get(Guid id, CancellationToken cancellationToken);
    Task<Guid> Create(CreateRoleQuery query, CancellationToken cancellationToken);
    Task Update(UpdateRoleQuery query, CancellationToken cancellationToken);
    Task Delete(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<RoleSummary>> ListAll(CancellationToken cancellationToken);
    Task<ListDataResult<RoleSummary>> List(ListRoleQuery query, CancellationToken cancellationToken);
    Task<PagingResult<RoleSummary>> ListTable(ListRoleQuery query, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RoleClaimSummary>> ListAllClaims(CancellationToken cancellationToken);
    Task<bool> RolesExsists(IReadOnlyCollection<string> roleNames, CancellationToken cancellationToken);
    Task<bool> RolesExsists(IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken);
}
