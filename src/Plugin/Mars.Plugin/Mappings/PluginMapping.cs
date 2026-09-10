using Mars.Plugin.Abstractions.Dto.Plugins;
using Mars.Plugin.Contracts.Plugins;
using Mars.Plugin.Dto;

namespace Mars.Plugin.Mappings;

internal static class PluginMapping
{
    public static PluginInfoDto ToInfoDto(this PluginInfo entity, bool pendingDelete = false)
        => new()
        {
            PackageId = entity.PackageId,
            Title = entity.Title,
            Version = entity.Version,
            Description = entity.Description,
            AssemblyName = entity.AssemblyFullName,
            Enabled = entity.Enabled,
            InstalledAt = entity.InstalledAt,
            FrontManifest = entity.ManifestFile,
            PackageTags = entity.PackageTags,
            RepositoryUrl = entity.RepositoryUrl,
            PackageIconUrl = string.IsNullOrEmpty(entity.PackageIcon) ? null : $"/_plugin/{entity.KeyName}/{entity.PackageIcon}",
            Source = entity.Source,
            Locked = entity.Locked,
            PendingDelete = pendingDelete,
        };
}
