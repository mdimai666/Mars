using Mars.Host.Shared.Dto.NavMenus;
using Mars.Shared.Common;

namespace Mars.Host.Shared.Repositories;

public interface INavMenuRepository : IDisposable
{
    Task<NavMenuSummary?> Get(Guid id, CancellationToken cancellationToken);
    Task<NavMenuDetail?> GetDetail(Guid id, CancellationToken cancellationToken);
    Task<Guid> Create(CreateNavMenuQuery query, CancellationToken cancellationToken);
    Task Update(UpdateNavMenuQuery query, CancellationToken cancellationToken);
    Task Delete(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<NavMenuSummary>> ListAll(ListAllNavMenuQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<NavMenuDetail>> ListAllActiveDetail(ListAllNavMenuQuery query, CancellationToken cancellationToken);
    Task<ListDataResult<NavMenuSummary>> List(ListNavMenuQuery query, CancellationToken cancellationToken);
    Task<PagingResult<NavMenuSummary>> ListTable(ListNavMenuQuery query, CancellationToken cancellationToken);
    Task<int> DeleteMany(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}
