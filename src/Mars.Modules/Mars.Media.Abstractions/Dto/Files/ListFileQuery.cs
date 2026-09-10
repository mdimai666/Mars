using Mars.Contracts.Common;

namespace Mars.Media.Abstractions.Dto.Files;

public record ListFileQuery : BasicListQuery
{
    /// <summary>
    /// Фильтр по папке. null — без фильтра (все файлы),
    /// Guid.Empty — файлы без папки (корень), иначе — файлы указанной папки.
    /// </summary>
    public Guid? FolderId { get; init; }
}
