using Mars.Host.Shared.Dto.Plugins;
using Mars.Shared.Common;
using Microsoft.AspNetCore.Http;

namespace Mars.Host.Shared.Services;

public interface IPluginService
{
    ListDataResult<PluginInfoDto> List(ListPluginQuery query);
    PagingResult<PluginInfoDto> ListTable(ListPluginQuery query);
    IDictionary<string, PluginManifestInfoDto> RuntimePluginManifests();
    Task<PluginsUploadOperationResultDto> UploadPlugin(IFormFileCollection files, CancellationToken cancellationToken);
}
