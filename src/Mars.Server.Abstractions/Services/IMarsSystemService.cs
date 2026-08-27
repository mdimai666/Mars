using Mars.Shared.Contracts.Systems;

namespace Mars.Host.Shared.Services;

public interface IMarsSystemService
{
    DateTimeOffset AppStartDateTime();
    string SystemUptime();
    long SystemUptimeMillis();
    SystemMinStatResponse SystemMinStat();
    IEnumerable<KeyValuePair<string, string>> AboutSystem();
    IEnumerable<KeyValuePair<string, string>> HostCacheSettings();
    string AppUptime();
    string MemoryUsage();
}
