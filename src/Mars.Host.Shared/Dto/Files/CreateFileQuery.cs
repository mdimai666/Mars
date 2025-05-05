namespace Mars.Host.Shared.Dto.Files;

public record CreateFileQuery
{
    public required string Name { get; init; }
    public required string FilePathFromUpload { get; init; }
    public ulong Size { get; init; }
    public required Guid UserId { get; init; }
    public required FileEntityMetaDto? Meta { get; init; }
}
