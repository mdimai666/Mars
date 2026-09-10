namespace Mars.Plugin.Abstractions.Dto.Plugins;

public record PluginInstallResultDto
{
    public required string PackageId { get; init; }
    public required string Version { get; init; }
    public required DateTimeOffset InstalledAtUtc { get; init; }
}
