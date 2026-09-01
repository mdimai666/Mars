using System.ComponentModel.DataAnnotations;

namespace Mars.Plugin.Contracts.Plugins;

public record SetPluginEnabledRequest
{
    [Required]
    public required string PackageId { get; init; }

    public required bool Enabled { get; init; }
}
