using Mars.Core.Exceptions;
using Mars.Contracts.Common;
using Mars.Contracts.NavMenus;

namespace Mars.WebApiClient.Interfaces;

public interface INavMenuServiceClient
{
    Task<NavMenuDetailResponse?> Get(Guid id);

    /// <summary>
    /// Создает
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="MarsValidationException"></exception>
    /// <exception cref="UserActionException"></exception>
    Task<Guid> Create(CreateNavMenuRequest request);
    Task Update(UpdateNavMenuRequest request);
    Task<ListDataResult<NavMenuSummaryResponse>> List(ListNavMenuQueryRequest filter);
    Task<PagingResult<NavMenuSummaryResponse>> ListTable(TableNavMenuQueryRequest filter);

    /// <summary>
    /// Список меню для админки: несохранённые системные меню подмешиваются виртуальной записью.
    /// </summary>
    Task<ListDataResult<NavMenuSummaryResponse>> ListForAdmin(ListNavMenuQueryRequest filter);

    Task Delete(Guid id);

    /// <summary>
    /// Сброс системного меню к дефолту (удаляет сохранённую копию из БД).
    /// </summary>
    Task<UserActionResult> Reset(Guid id);

    Task<UserActionResult> Import(Guid id, string json);
    Task DeleteMany(Guid[] ids);
}
