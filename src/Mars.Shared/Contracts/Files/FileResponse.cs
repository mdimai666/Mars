namespace Mars.Shared.Contracts.Files;

public record FileSummaryResponse
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
public record FileDetailResponse : FileSummaryResponse
{
    public required DateTimeOffset? ModifiedAt { get; init; }
    public required string FilePhysicalPath { get; init; }
    public required string FileVirtualPath { get; init; }
    public required virtual FileEntityMetaResponse Meta { get; init; }
    public required Guid UserId { get; init; }
}

public record FileEntityMetaResponse
{
    public required ImageInfoResponse? ImageInfo { get; init; }
    public required Dictionary<string, ImageThumbnailResponse>? Thumbnails { get; init; }
}

public record ImageInfoResponse
{
    public required int Width { get; init; }
    public required int Height { get; init; }
}

public record ImageThumbnailResponse
{
    public required string Name { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required string FilePath { get; init; }
    public required string FileUrl { get; init; }

}

public record FileListItemResponse : FileSummaryResponse
{
    public required string FilePhysicalPath { get; init; }
    public required string FileVirtualPath { get; init; }
}
