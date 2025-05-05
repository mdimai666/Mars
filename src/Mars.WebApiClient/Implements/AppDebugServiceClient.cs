using Mars.Shared.Common;
using Mars.WebApiClient.Interfaces;
using Flurl.Http;

namespace Mars.WebApiClient.Implements;

internal class AppDebugServiceClient : BasicServiceClient, IAppDebugServiceClient
{
    public AppDebugServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "AppDebug";
    }

    public Task<UserActionResult<string>> GetLogs(string filename, int lines = 1000)
        => _client.Request($"{_basePath}{_controllerName}", "GetLogs")
                    .AppendQueryParam(new { filename, lines })
                    .GetJsonAsync<UserActionResult<string>>();

    public Task<IReadOnlyCollection<string>> LogFiles()
        => _client.Request($"{_basePath}{_controllerName}", "LogFiles")
                    .GetJsonAsync<IReadOnlyCollection<string>>();
}
