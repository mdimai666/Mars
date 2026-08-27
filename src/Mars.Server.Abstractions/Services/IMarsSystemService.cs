using Mars.Contracts.Systems;

namespace Mars.Server.Abstractions.Services;

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
