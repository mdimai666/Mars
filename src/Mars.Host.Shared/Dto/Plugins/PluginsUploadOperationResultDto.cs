namespace Mars.Host.Shared.Dto.Plugins;

public record PluginsUploadOperationResultDto
{
    public required DateTimeOffset UploadStart { get; init; }
    public required DateTimeOffset UploadEnd { get; init; }
    public required IReadOnlyCollection<PluginsUploadOperationItemDto> Items { get; init; }
}

public record PluginsUploadOperationItemDto
{
    public bool Success => ErrorMessage == null;
    public required string FileName { get; init; }
    public required long FileSize { get; init; }
    public required string? ErrorMessage { get; init; }
}
