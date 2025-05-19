namespace Mars.Shared.Contracts.Plugins;

public record PluginInfoResponse
{
    public required string PackageId { get; set; }
    public required string Title { get; set; }
    public required string? Description { get; set; }
    public required string Version { get; set; }
    public required string AssemblyName { get; set; }
    public required bool Enabled { get; set; }
    public required DateTimeOffset InstalledAt { get; set; }
    public required string? FrontManifest { get; set; }
}
