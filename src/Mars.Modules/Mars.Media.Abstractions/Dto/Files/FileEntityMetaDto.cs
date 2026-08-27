namespace Mars.Host.Shared.Dto.Files;

public record FileEntityMetaDto
{
    public required ImageInfoDto? ImageInfo { get; init; }
    public required Dictionary<string, ImageThumbnailDto>? Thumbnails { get; init; }
}

public record ImageInfoDto
{
    public required int Width { get; init; }
    public required int Height { get; init; }
}

public record ImageThumbnailDto
{
    public required string Name { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required string FilePath { get; init; }
    public required string FileUrl { get; init; }

}
