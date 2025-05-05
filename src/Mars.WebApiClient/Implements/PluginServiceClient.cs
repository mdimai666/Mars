using Mars.Shared.Common;
using Mars.Shared.Contracts.Plugins;
using Mars.WebApiClient.Interfaces;
using Flurl.Http;

namespace Mars.WebApiClient.Implements;

internal class PluginServiceClient : BasicServiceClient, IPluginServiceClient
{
    public PluginServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "Plugin";
    }

    public Task<ListDataResult<PluginInfoResponse>> List(ListPluginQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<ListDataResult<PluginInfoResponse>>();
    
    public Task<PagingResult<PluginInfoResponse>> ListTable(TablePluginQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/ListTable")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<PagingResult<PluginInfoResponse>>();

}
