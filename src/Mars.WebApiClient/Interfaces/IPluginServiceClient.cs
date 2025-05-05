using Mars.Shared.Common;
using Mars.Shared.Contracts.Plugins;

namespace Mars.WebApiClient.Interfaces;

public interface IPluginServiceClient
{
    Task<ListDataResult<PluginInfoResponse>> List(ListPluginQueryRequest filter);
    Task<PagingResult<PluginInfoResponse>> ListTable(TablePluginQueryRequest filter);

}
