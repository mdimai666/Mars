using Flurl.Http;
using Mars.Contracts.Common;
using Mars.WebApiClient.Interfaces;

namespace Mars.WebApiClient.Implements;

internal class AppDebugServiceClient : BasicServiceClient, IAppDebugServiceClient
{
    public AppDebugServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "AppDebug";
    }

    public Task<UserActionResult<string>> GetLogs(int lines = 1000, IReadOnlyCollection<string>? levels = null, string? period = null)
        => _client.Request($"{_basePath}{_controllerName}", "GetLogs")
                    .AppendQueryParam(new
                    {
                        lines,
                        levels = levels is null ? "" : string.Join(",", levels),
                        period = period ?? "",
                    })
                    .GetJsonAsync<UserActionResult<string>>();

    public Task<IReadOnlyCollection<string>> LogFiles()
        => _client.Request($"{_basePath}{_controllerName}", "LogFiles")
                    .GetJsonAsync<IReadOnlyCollection<string>>();
}
