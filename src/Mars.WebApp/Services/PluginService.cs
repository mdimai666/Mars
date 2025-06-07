//#define USE_EXAMPLE_PLUGINS

using Mars.Core.Extensions;
using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.Plugins;
using Mars.Host.Shared.Services;
using Mars.Plugin;
using Mars.Shared.Common;

namespace Mars.Services;

internal class PluginService : IPluginService
{
    public PluginService()
    {
    }

    public ListDataResult<PluginInfoDto> List(ListPluginQuery query)
    {
#if USE_EXAMPLE_PLUGINS
        return GetExamplePluginList(query).AsListDataResult(query);
#endif

        return ApplicationPluginExtensions.Plugins
            .Where(s => (query.Search == null || s.Info.Title.Contains(query.Search, StringComparison.OrdinalIgnoreCase)))
            .Select(s => GetPluginInfoDto(s.Info))
            .AsListDataResult(query);
    }

    public PagingResult<PluginInfoDto> ListTable(ListPluginQuery query)
    {
#if USE_EXAMPLE_PLUGINS
        return GetExamplePluginList(query).AsPagingResult(query);
#endif
        return ApplicationPluginExtensions.Plugins
            .Where(s => (query.Search == null || s.Info.Title.Contains(query.Search, StringComparison.OrdinalIgnoreCase)))
            .Select(s => GetPluginInfoDto(s.Info))
            .AsPagingResult(query);
    }

    public IDictionary<string, PluginManifestInfoDto> RuntimePluginManifests()
    {
        return ApplicationPluginExtensions.Plugins.Where(s => s.Info.ManifestFile != null)
                                                    .Select(s => new PluginManifestInfoDto { Name = s.Info.KeyName, Uri = s.Info.ManifestFile! })
                                                    .ToDictionary(s => s.Name);
    }

    public static PluginInfoDto GetPluginInfoDto(PluginInfo pluginInfo)
    {
        return new PluginInfoDto()
        {
            PackageId = pluginInfo.PackageId,
            Title = pluginInfo.Title,
            Version = pluginInfo.Version,
            Description = pluginInfo.Description,
            AssemblyName = pluginInfo.AssemblyFullName,
            Enabled = true,
            InstalledAt = DateTimeOffset.MinValue,
            FrontManifest = pluginInfo.ManifestFile,
            PackageTags = pluginInfo.PackageTags,
            RepositoryUrl = pluginInfo.RepositoryUrl,
            PackageIconUrl = string.IsNullOrEmpty(pluginInfo.PackageIcon) ? null : $"/_plugin/{pluginInfo.KeyName}/{pluginInfo.PackageIcon}",
        };
    }

    #region EXAMPLE ROWS
    public static List<PluginInfoDto> GetExamplePluginList(ListPluginQuery query)
    {
        List<PluginInfoDto> pluginsExample = [
            new(){
                PackageId = "plugin.25122404-8a2b-46e9-93dc-ff2949a8fe0e",
                Title = "Plugin 1",
                Version = "1.0.0",
                AssemblyName = "Mars.EShop",
                Description = "Плагин магазина ",
                Enabled = true,
                InstalledAt = DateTimeOffset.MinValue,
                FrontManifest = "_plugin/Mars.EShop/_front_plugins.json",
                PackageTags = ["eshop"],
                RepositoryUrl = null,
                PackageIconUrl = null,
            },
            new(){
                PackageId = "google.zsn.bu",
                Title = "Plugin 3b839c4d-ff09-4f2b-98eb-052f73de5c6a",
                Version = "1.0.0",
                AssemblyName = "AAAAAA.SDSDSIDISJ.sDSPOKD",
                Description = "Плагин магазина  sajdlkjkldfja;ldjf;kadf k;da",
                Enabled = true,
                InstalledAt = DateTimeOffset.Now,
                FrontManifest = null,
                PackageTags = [],
                RepositoryUrl = null,
                PackageIconUrl = null,
            },
            new(){
                PackageId = "askdasdsd.sdasdfd.dafadf",
                Title = "Plugin EShop Some goog things1",
                Version = "2.0.0-aplha1",
                AssemblyName = "Mars.EShop.SDSDij SDJ",
                Description = "Плагин магазина as dfkda;l fk;ldka fl;kdaf d765b36b-ead3-44d2-af1a-5b2ecb75e567",
                Enabled = true,
                InstalledAt = DateTimeOffset.Now + TimeSpan.FromDays(-5),
                FrontManifest = null,
                PackageTags = ["nodes", "eshop"],
                RepositoryUrl = null,
                PackageIconUrl = null,
            },
        ];
        return pluginsExample.Where(s => (query.Search == null || (
                                            s.Title.Contains(query.Search, StringComparison.OrdinalIgnoreCase)
                                            || s.PackageId.Contains(query.Search, StringComparison.OrdinalIgnoreCase)
                                            || (s.Description?.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ?? false)
                                            ))
                                    )
            .OrderBySortStringParam(query.Sort ?? "Title")
            .ToList();
    }
    #endregion

}
