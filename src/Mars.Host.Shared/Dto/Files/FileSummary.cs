namespace Mars.Host.Shared.Dto.Files;

public record FileSummary
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string Name { get; init; }
    public required ulong Size { get; init; }

    /// <summary>
    /// Расширение файла. Без ведущей точки.
    /// </summary>
    public required string Ext { get; init; }
    public required string Url { get; init; }
    public required string UrlRelative { get; init; }
    public required string? PreviewIcon { get; init; }
    public required bool IsImage { get; init; }
    public required bool IsSvg { get; init; }

}
