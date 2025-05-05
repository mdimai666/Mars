using Mars.Shared.Contracts.Systems;
using Mars.WebApiClient.Interfaces;
using Flurl.Http;

namespace Mars.WebApiClient.Implements;

internal class SystemServiceClient : BasicServiceClient, ISystemServiceClient
{
    public SystemServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "System";
    }

    public Task<string> HealthCheck()
        => _client.Request($"{_basePath}{_controllerName}", "HealthCheck")
                    .GetStringAsync();

    public Task<string> HealthCheck2()
        => _client.Request($"{_basePath}{_controllerName}", "HealthCheck2")
                    .GetStringAsync();

    public Task<IReadOnlyCollection<KeyValuePair<string, string>>> AboutSystem()
        => _client.Request($"{_basePath}{_controllerName}", "AboutSystem")
                    .GetJsonAsync<IReadOnlyCollection<KeyValuePair<string, string>>>();

    public Task<string> AppUptime()
        => _client.Request($"{_basePath}{_controllerName}", "AppUptime")
                    .GetStringAsync();

    public Task<DateTime> AppStartDateTime()
        => _client.Request($"{_basePath}{_controllerName}", "AppStartDateTime")
                    .GetJsonAsync<DateTime>();

    public Task<IReadOnlyCollection<KeyValuePair<string, string>>> HostCacheSettings()
        => _client.Request($"{_basePath}{_controllerName}", "HostCacheSettings")
                    .GetJsonAsync<IReadOnlyCollection<KeyValuePair<string, string>>>();

    public Task<string> MemoryUsage()
        => _client.Request($"{_basePath}{_controllerName}", "MemoryUsage")
                    .GetStringAsync();

    public Task<SystemMinStatResponse> SystemMinStat()
        => _client.Request($"{_basePath}{_controllerName}", "SystemMinStat")
                    .GetJsonAsync<SystemMinStatResponse>();

    public Task<string> SystemUptime()
        => _client.Request($"{_basePath}{_controllerName}", "SystemUptime")
                    .GetStringAsync();

    public Task<long> SystemUptimeMillis()
        => _client.Request($"{_basePath}{_controllerName}", "SystemUptimeMillis")
                    .GetJsonAsync<long>();
}
