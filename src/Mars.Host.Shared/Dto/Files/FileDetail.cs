namespace Mars.Host.Shared.Dto.Files;

public record FileDetail : FileSummary
{
    public required DateTimeOffset? ModifiedAt { get; init; }
    public required string FilePhysicalPath { get; init; }
    public required string FileVirtualPath { get; init; }
    public required FileEntityMetaDto Meta { get; init; }
    public required Guid UserId { get; init; }
}
