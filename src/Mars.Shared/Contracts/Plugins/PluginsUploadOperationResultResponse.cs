namespace Mars.Shared.Contracts.Plugins;

public record PluginsUploadOperationResultResponse
{
    public required DateTimeOffset UploadStart { get; init; }
    public required DateTimeOffset UploadEnd { get; init; }
    public required IReadOnlyCollection<PluginsUploadOperationItemResponse> Items { get; init; }
}

public record PluginsUploadOperationItemResponse
{
    public bool Success => ErrorMessage == null;
    public required string FileName { get; init; }
    public required long FileSize { get; init; }
    public required string? ErrorMessage { get; init; }
}
