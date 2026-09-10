using Mars.Contracts.Common;
using Mars.Core.Exceptions;
using Mars.Media.Contracts.Files;

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
    Task<FileDetailResponse> Upload(Stream stream, string fileName, Guid? folderId = null, string? folderPath = null);
    //Task Update(UpdateNavMenuRequest request);
    Task<ListDataResult<FileListItemResponse>> List(ListFileQueryRequest filter);
    Task<PagingResult<FileListItemResponse>> ListTable(TableFileQueryRequest filter);
    Task Delete(Guid id);
    Task DeleteMany(Guid[] ids);

    /// <summary>Папки непосредственно в указанном родителе (null — верхний уровень Media)</summary>
    Task<List<FolderResponse>> ListFolders(Guid? parentId);
    /// <summary>Цепочка папок от корня до указанной (для хлебных крошек)</summary>
    Task<List<FolderResponse>> FolderBreadcrumbs(Guid folderId);
    Task<FolderResponse> CreateFolder(CreateFolderRequest request);
    Task<FolderResponse> RenameFolder(Guid id, RenameFolderRequest request);
    Task DeleteFolder(Guid id);
    Task<UserActionResult> MoveFiles(MoveFilesRequest request);
}
