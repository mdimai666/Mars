using Flurl.Http;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Plugins;
using Mars.WebApiClient.Interfaces;

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

    public Task<PluginsUploadOperationResultResponse> UploadPlugin(params IReadOnlyCollection<(Stream file, string filename)> files)
    => _client.Request($"{_basePath}{_controllerName}", "UploadPlugin")
               .PostMultipartAsync(mp =>
               {
                   foreach (var (file, filename) in files)
                       mp.AddFile("files", file, filename, "application/zip");
               })
               .ReceiveJson<PluginsUploadOperationResultResponse>();
}
