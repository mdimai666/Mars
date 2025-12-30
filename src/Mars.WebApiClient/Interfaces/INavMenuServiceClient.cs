using Mars.Core.Exceptions;
using Mars.Shared.Common;
using Mars.Shared.Contracts.NavMenus;

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
    Task Delete(Guid id);
    Task<UserActionResult> Import(Guid id, string json);
    Task DeleteMany(Guid[] ids);
}
