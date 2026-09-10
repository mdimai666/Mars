using System.ComponentModel.DataAnnotations;

namespace Mars.Plugin.Contracts.Plugins;

public record InstallPluginRequest
{
    [Required]
    public required string PackageId { get; init; }

    /// <summary>
    /// Конкретная версия; если не задана — ставится последняя доступная.
    /// </summary>
    public string? Version { get; init; }
}
