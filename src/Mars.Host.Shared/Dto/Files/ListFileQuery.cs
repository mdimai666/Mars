using Mars.Shared.Common;

namespace Mars.Host.Shared.Dto.Files;

public record ListFileQuery : BasicListQuery
{
    /// <summary>
    /// Фильтр по папке. null — без фильтра (все файлы),
    /// Guid.Empty — файлы без папки (корень), иначе — файлы указанной папки.
    /// </summary>
    public Guid? FolderId { get; init; }
}
