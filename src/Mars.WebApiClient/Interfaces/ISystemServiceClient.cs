using Mars.Shared.Contracts.Systems;

namespace Mars.WebApiClient.Interfaces;

public interface ISystemServiceClient
{
    Task<string> HealthCheck();
    Task<string> HealthCheck2();
    Task<string> MemoryUsage();
    Task<string> AppUptime();
    Task<DateTime> AppStartDateTime();
    Task<string> SystemUptime();
    Task<long> SystemUptimeMillis();
    Task<SystemMinStatResponse> SystemMinStat();
    Task<IReadOnlyCollection<KeyValuePair<string, string>>> AboutSystem();
    Task<IReadOnlyCollection<KeyValuePair<string, string>>> HostCacheSettings();

}
