namespace Mars.Plugin.Abstractions.Dto.Plugins;

public record PluginManifestInfoDto
{
    public required string Name { get; init; }
    public required string Uri { get; init; }
}
