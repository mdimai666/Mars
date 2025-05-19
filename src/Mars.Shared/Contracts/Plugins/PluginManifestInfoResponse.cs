namespace Mars.Shared.Contracts.Plugins;

public record PluginManifestInfoResponse
{
    public required string Name { get; set; }
    public required string Uri { get; set; }
}
