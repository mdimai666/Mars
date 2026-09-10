namespace Mars.Media.Abstractions.Dto.Files;

public record MoveFilesQuery
{
    public required IReadOnlyCollection<Guid> Ids { get; init; }

    /// <summary>
    /// Целевая папка. null — переместить в корень (Media).
    /// </summary>
    public Guid? FolderId { get; init; }
}
