namespace Mars.Shared.Contracts.Plugins;

public record PluginManifestInfoResponse
{
    public required string Name { get; init; }
    public required string Uri { get; init; }
}
