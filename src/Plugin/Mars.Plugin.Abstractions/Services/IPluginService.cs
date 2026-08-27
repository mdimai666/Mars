using Mars.Plugin.Abstractions.Dto.Plugins;
using Mars.Contracts.Common;
using Microsoft.AspNetCore.Http;

namespace Mars.Plugin.Abstractions.Services;

public interface IPluginService
{
    ListDataResult<PluginInfoDto> List(ListPluginQuery query);
    PagingResult<PluginInfoDto> ListTable(ListPluginQuery query);
    IDictionary<string, PluginManifestInfoDto> RuntimePluginManifests();
    Task<PluginsUploadOperationResultDto> UploadPlugin(IFormFileCollection files, CancellationToken cancellationToken);
}
