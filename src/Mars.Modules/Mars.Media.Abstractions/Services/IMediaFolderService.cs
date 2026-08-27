using Mars.Media.Abstractions.Dto.Files;
using Mars.Media.Abstractions.Services;
using Mars.Contracts.Common;

namespace Mars.Media.Abstractions.Services;

/// <summary>
/// Папки медиа. DB-first: папка существует только при наличии записи в БД,
/// физический каталог создаётся/перемещается/удаляется вместе с ней.
/// </summary>
public interface IMediaFolderService
{
    /// <summary>Список папок непосредственно в указанном родителе (null — верхний уровень Media)</summary>
    Task<List<MediaFolderDto>> ListFolders(Guid? parentId, CancellationToken cancellationToken);

    /// <summary>Цепочка папок от корня до указанной (для хлебных крошек), включая саму папку</summary>
    Task<List<MediaFolderDto>> GetBreadcrumbs(Guid folderId, CancellationToken cancellationToken);

    Task<MediaFolderDto?> GetById(Guid id, CancellationToken cancellationToken);

    /// <summary>Найти папку по пути или создать (с физическим каталогом). Используется для авто-папок вида Media/{год}</summary>
    Task<MediaFolderDto> GetOrCreateByPath(string path, Guid userId, CancellationToken cancellationToken);

    Task<MediaFolderDto> Create(CreateFolderQuery query, CancellationToken cancellationToken);

    /// <summary>Переименование «на месте» (родитель не меняется)</summary>
    Task<MediaFolderDto> Rename(Guid id, string newName, CancellationToken cancellationToken);

    /// <summary>Удаление пустой папки</summary>
    Task Delete(Guid id, CancellationToken cancellationToken);

    /// <summary>Переместить файлы в папку (FolderId = null — в корень)</summary>
    Task<UserActionResult> MoveFiles(MoveFilesQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Зарегистрировать физические каталоги как папки в БД (используется ScanFiles).
    /// Возвращает карту путь→Id всех папок (включая уже существовавшие).
    /// </summary>
    Task<Dictionary<string, Guid>> EnsureFoldersByPaths(IEnumerable<string> dirPaths, Guid userId, CancellationToken cancellationToken);
}
