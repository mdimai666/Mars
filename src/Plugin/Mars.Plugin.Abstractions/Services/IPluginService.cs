using Mars.Contracts.Common;
using Mars.Plugin.Abstractions.Dto.Plugins;
using Microsoft.AspNetCore.Http;

namespace Mars.Plugin.Abstractions.Services;

public interface IPluginService
{
    ListDataResult<PluginInfoDto> List(ListPluginQuery query);
    PagingResult<PluginInfoDto> ListTable(ListPluginQuery query);
    IDictionary<string, PluginManifestInfoDto> RuntimePluginManifests();
    Task<PluginsUploadOperationResultDto> UploadPlugin(IFormFileCollection files, CancellationToken cancellationToken);
    Task<PluginInstallResultDto> InstallFromNuget(string packageId, string? version, CancellationToken cancellationToken);
    Task SetEnabled(string packageId, bool enabled);
    Task Uninstall(string packageId);
}
