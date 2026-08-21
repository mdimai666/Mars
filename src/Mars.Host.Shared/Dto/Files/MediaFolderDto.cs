namespace Mars.Host.Shared.Dto.Files;

public record MediaFolderDto
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string Name { get; init; }

    /// <summary>
    /// Физический путь папки от upload, например Media/2026
    /// </summary>
    public required string Path { get; init; }
    public Guid? ParentId { get; init; }
    public required Guid CreatedBy { get; init; }
    public string? Icon { get; init; }

    /// <summary>
    /// Количество файлов непосредственно в папке (без вложенных папок)
    /// </summary>
    public int FilesCount { get; init; }
}
