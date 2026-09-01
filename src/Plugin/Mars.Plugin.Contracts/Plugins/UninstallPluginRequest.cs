using System.ComponentModel.DataAnnotations;

namespace Mars.Plugin.Contracts.Plugins;

public record UninstallPluginRequest
{
    [Required]
    public required string PackageId { get; init; }
}
