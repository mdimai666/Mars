namespace Mars.Media.Abstractions.Dto.Files;

public record ListAllFileQuery
{
    public bool? IsImage { get; init; }
    public IReadOnlyCollection<Guid>? Ids { get; init; }

    /// <summary>
    /// Фильтр по папке. null — без фильтра,
    /// Guid.Empty — файлы без папки (корень), иначе — файлы указанной папки.
    /// </summary>
    public Guid? FolderId { get; init; }
}
