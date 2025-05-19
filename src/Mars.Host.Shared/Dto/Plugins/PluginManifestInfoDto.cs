namespace Mars.Host.Shared.Dto.Plugins;

public record PluginManifestInfoDto
{
    public required string Name { get; set; }
    public required string Uri { get; set; }
}
