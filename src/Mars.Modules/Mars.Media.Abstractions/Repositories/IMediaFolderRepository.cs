using Mars.Contracts.Dto.Files;
using Mars.Media.Abstractions.Dto.Files;
using Mars.Media.Abstractions.Repositories;

namespace Mars.Media.Abstractions.Repositories;

public interface IMediaFolderRepository
{
    Task<List<MediaFolderDto>> ListByParent(Guid? parentId, CancellationToken cancellationToken);
    Task<List<MediaFolderDto>> ListAll(CancellationToken cancellationToken);
    Task<List<MediaFolderDto>> GetBreadcrumbs(Guid folderId, CancellationToken cancellationToken);
    Task<MediaFolderDto?> GetById(Guid id, CancellationToken cancellationToken);
    Task<MediaFolderDto?> GetByPath(string path, CancellationToken cancellationToken);
    Task<bool> ExistsByParentAndName(Guid? parentId, string name, CancellationToken cancellationToken);
    Task<bool> HasChildren(Guid id, CancellationToken cancellationToken);
    Task<Guid> Create(CreateFolderQuery query, CancellationToken cancellationToken);
    Task Delete(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Переименование папки: переписать пути папки и всех вложенных папок,
    /// пути файлов и их миниатюр. Один SaveChanges.
    /// </summary>
    /// <param name="thumbPrefixes">перепись префиксов путей миниатюр (null — миниатюры не трогать)</param>
    Task ApplyRename(
        Guid folderId,
        string newName,
        string oldPath,
        string newPath,
        (string OldPrefix, string NewPrefix)? thumbPrefixes,
        FileHostingInfo hostingInfo,
        CancellationToken cancellationToken);
}
