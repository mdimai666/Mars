namespace Mars.Host.Shared.Dto.Files;

public record FileListItem : FileSummary
{
    public required string FilePhysicalPath { get; init; }
    public required string FileVirtualPath { get; init; }
}
