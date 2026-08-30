namespace Mars.Media.Abstractions.Dto.Files;

/// <summary>
/// Обновление файла после перемещения/переименования папки
/// </summary>
public record FileMoveUpdate
{
    public required Guid Id { get; init; }
    public required string FilePhysicalPath { get; init; }
    public required string FileVirtualPath { get; init; }
    public Guid? FolderId { get; init; }

    /// <summary>
    /// null — мету не менять
    /// </summary>
    public FileEntityMetaDto? Meta { get; init; }
}
