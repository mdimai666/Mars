namespace Mars.Shared.Contracts.Plugins;

public record PluginInfoResponse
{
    public required string PackageId { get; init; }
    public required string Title { get; init; }
    public required string? Description { get; init; }
    public required string Version { get; init; }
    public required string AssemblyName { get; init; }
    public required bool Enabled { get; init; }
    public required DateTimeOffset InstalledAt { get; init; }
    public required string? FrontManifest { get; init; }
    public required string[] PackageTags { get; init; }
    public required string? RepositoryUrl { get; init; }
    public required string? PackageIconUrl { get; init; }
}
