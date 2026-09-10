namespace Mars.Plugin.Contracts.Plugins;

public record PluginInstallResponse
{
    public required string PackageId { get; init; }
    public required string Version { get; init; }
    public required DateTimeOffset InstalledAtUtc { get; init; }
    public required bool RestartRequired { get; init; }
    public required string Message { get; init; }
}
