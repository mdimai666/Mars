using Mars.Cms.Abstractions.Dto.NavMenus;
using Mars.Contracts.Common;

namespace Mars.Cms.Abstractions.Services;

/// <summary>
/// Singletone service
/// </summary>
public interface INavMenuService
{
    Task<NavMenuSummary?> Get(Guid id, CancellationToken cancellationToken);
    Task<NavMenuDetail?> GetDetail(Guid id, CancellationToken cancellationToken);
    Task<ListDataResult<NavMenuSummary>> List(ListNavMenuQuery query, CancellationToken cancellationToken);
    Task<PagingResult<NavMenuSummary>> ListTable(ListNavMenuQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Список меню для админки: несохранённые системные меню подмешиваются виртуальной записью.
    /// </summary>
    Task<ListDataResult<NavMenuSummary>> ListForAdmin(ListNavMenuQuery query, CancellationToken cancellationToken);

    Task<Guid> Create(CreateNavMenuQuery query, CancellationToken cancellationToken);
    Task Update(UpdateNavMenuQuery query, CancellationToken cancellationToken);
    Task<NavMenuSummary> Delete(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<NavMenuSummary>> DeleteMany(DeleteManyNavMenuQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Сброс системного меню к дефолту (удаляет сохранённую в БД копию).
    /// </summary>
    Task Reset(Guid id, CancellationToken cancellationToken);

    NavMenuDetail DevMenu();
    IReadOnlyCollection<NavMenuDetail> GetAppInitialDataMenus(bool includeDevMenu = false);

    Task<NavMenuExport> Export(Guid id);
    Task<UserActionResult> Import(Guid id, NavMenuImport navMenu);
}
