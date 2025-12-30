using Mars.Host.Shared.Dto.NavMenus;
using Mars.Shared.Common;

namespace Mars.Host.Shared.Services;

/// <summary>
/// Singletone service
/// </summary>
public interface INavMenuService
{
    Task<NavMenuSummary?> Get(Guid id, CancellationToken cancellationToken);
    Task<NavMenuDetail?> GetDetail(Guid id, CancellationToken cancellationToken);
    Task<ListDataResult<NavMenuSummary>> List(ListNavMenuQuery query, CancellationToken cancellationToken);
    Task<PagingResult<NavMenuSummary>> ListTable(ListNavMenuQuery query, CancellationToken cancellationToken);
    Task<Guid> Create(CreateNavMenuQuery query, CancellationToken cancellationToken);
    Task Update(UpdateNavMenuQuery query, CancellationToken cancellationToken);
    Task<NavMenuSummary> Delete(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<NavMenuSummary>> DeleteMany(DeleteManyNavMenuQuery query, CancellationToken cancellationToken);

    NavMenuDetail DevMenu();
    IReadOnlyCollection<NavMenuDetail> GetAppInitialDataMenus(bool includeDevMenu = false);

    Task<NavMenuExport> Export(Guid id);
    Task<UserActionResult> Import(Guid id, NavMenuImport navMenu);
}
