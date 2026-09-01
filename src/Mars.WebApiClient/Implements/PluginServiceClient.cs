using Flurl.Http;
using Mars.Contracts.Common;
using Mars.Plugin.Contracts.Plugins;
using Mars.WebApiClient.Interfaces;

namespace Mars.WebApiClient.Implements;

internal class PluginServiceClient : BasicServiceClient, IPluginServiceClient
{
    public PluginServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "Plugin";
    }

    public Task<ListDataResult<PluginInfoResponse>> List(ListPluginQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/list/offset")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<ListDataResult<PluginInfoResponse>>();

    public Task<PagingResult<PluginInfoResponse>> ListTable(TablePluginQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/list/page")
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

    public Task<PluginInstallResponse> InstallFromNuget(string packageId, string? version = null)
        => _client.Request($"{_basePath}{_controllerName}", "InstallFromNuget")
                  .PostJsonAsync(new InstallPluginRequest { PackageId = packageId, Version = version })
                  .ReceiveJson<PluginInstallResponse>();

    public Task SetEnabled(string packageId, bool enabled)
        => _client.Request($"{_basePath}{_controllerName}", "SetEnabled")
                  .PostJsonAsync(new SetPluginEnabledRequest { PackageId = packageId, Enabled = enabled });

    public Task Uninstall(string packageId)
        => _client.Request($"{_basePath}{_controllerName}", "Uninstall")
                  .PostJsonAsync(new UninstallPluginRequest { PackageId = packageId });
}
