using Mars.Contracts.Common;
using Mars.Plugin.Contracts.Plugins;

namespace Mars.WebApiClient.Interfaces;

public interface IPluginServiceClient
{
    Task<ListDataResult<PluginInfoResponse>> List(ListPluginQueryRequest filter);
    Task<PagingResult<PluginInfoResponse>> ListTable(TablePluginQueryRequest filter);
    Task<PluginsUploadOperationResultResponse> UploadPlugin(params IReadOnlyCollection<(Stream file, string filename)> files);
}
