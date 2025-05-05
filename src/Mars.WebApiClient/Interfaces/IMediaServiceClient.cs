using Mars.Core.Exceptions;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Files;

namespace Mars.WebApiClient.Interfaces;

public interface IMediaServiceClient
{
    Task<FileDetailResponse?> Get(Guid id);

    /// <summary>
    /// Создает
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="MarsValidationException"></exception>
    /// <exception cref="UserActionException"></exception>
    Task<FileDetailResponse> Upload(Stream stream, string fileName);
    //Task Update(UpdateNavMenuRequest request);
    Task<ListDataResult<FileListItemResponse>> List(ListFileQueryRequest filter);
    Task<PagingResult<FileListItemResponse>> ListTable(TableFileQueryRequest filter);
    Task Delete(Guid id);
    Task<UserActionResult> ExecuteAction(ExecuteActionRequest action);

}
