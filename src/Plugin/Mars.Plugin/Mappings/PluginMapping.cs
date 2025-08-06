using Mars.Host.Shared.Dto.Plugins;
using Mars.Plugin.Dto;

namespace Mars.Plugin.Mappings;

internal static class PluginMapping
{
    public static PluginInfoDto ToInfoDto(this PluginInfo entity)
        => new()
        {
            PackageId = entity.PackageId,
            Title = entity.Title,
            Version = entity.Version,
            Description = entity.Description,
            AssemblyName = entity.AssemblyFullName,
            Enabled = true,
            InstalledAt = DateTimeOffset.MinValue,
            FrontManifest = entity.ManifestFile,
            PackageTags = entity.PackageTags,
            RepositoryUrl = entity.RepositoryUrl,
            PackageIconUrl = string.IsNullOrEmpty(entity.PackageIcon) ? null : $"/_plugin/{entity.KeyName}/{entity.PackageIcon}",
        };

    public static IReadOnlyCollection<PluginInfoDto> ToInfoDto(this IEnumerable<PluginInfo> entities)
        => entities.Select(ToInfoDto).ToArray();
}
