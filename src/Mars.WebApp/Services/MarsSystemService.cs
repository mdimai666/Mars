using System.Diagnostics;
using System.Reflection;
using Humanizer;
using Mars.Core.Extensions;
using Mars.Host.Shared.Services;
using Mars.Shared.Contracts.Systems;
using Mars.UseStartup;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;

namespace Mars.Services;

internal class MarsSystemService : IMarsSystemService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IOptionService _optionService;
    static List<KeyValuePair<string, string>>? cachedAboutSystem;

    public MarsSystemService(IMemoryCache memoryCache, IOptionService optionService)
    {
        _memoryCache = memoryCache;
        _optionService = optionService;
    }

    public IEnumerable<KeyValuePair<string, string>> AboutSystem()
    {
        if (cachedAboutSystem == null)
        {
            cachedAboutSystem = new();
            var d = cachedAboutSystem;

            var assembly = Assembly.GetExecutingAssembly();
            //string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            string version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
            var os = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
            var osArch = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture;
            var RuntimeIdentifier = System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier;
            var FrameworkDescription = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            var ProcessArchitecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture;

            Action<string, string> add = (k, v) => d.Add(new KeyValuePair<string, string>(k, v));

            add("Mars", version);
            add("OS", os);
            add("Arch", osArch.ToString());
            add("RuntimeIdentifier", RuntimeIdentifier);
            add("FrameworkDescription", FrameworkDescription);
            add("ProcessArchitecture", ProcessArchitecture.ToString());
            add("", "");
            //add("ParentProcess", ParentProcess());
            add("Environment", MarsStartupInfo.ASPNETCORE_ENVIRONMENT);
            add("IsPM2", IsPM2().ToString());
            add("IsRunningInDocker", MarsStartupInfo.IsRunningInDocker.ToString());

            var timezone = GetTimeZone().ConfigureAwait(false).GetAwaiter().GetResult();

            add("ServerTimeZone", timezone.ServerTimeZone.DisplayName);
            add("DatabaseTimeZone", timezone.DatabaseTimeZone.DisplayName);


        }
        return cachedAboutSystem;
    }

    string ParentProcess()
    {
        return "";
    }
    bool IsPM2()
    {
        //var process = Process.GetCurrentProcess();
        //var processName = process.ProcessName;
        //var list = Process.GetProcesses().ToList();

        //List<string> names = new();

        //var pid = process.Id;
        //process.Parent
        //do
        //{
        //    var parent = list.FirstOrDefault(s=>s.)
        //} while (true);

        //return "x";

        //var env = serviceProvider.GetRequiredService<IHostEnvironment>();

        string p1 = "pm_id";
        string p2 = "PM2_USAGE";

        var ev = Environment.GetEnvironmentVariables();
        return ev.Contains(p1) && ev.Contains(p2);

        //var dd = Environment.GetEnvironmentVariables().Cast<DictionaryEntry>()
        //    .Select(x => (string)x.Key + "=" + (string)x.Value);
        //var s = string.Join(";\n", dd);
        //return s;
    }

    public string AppUptime()
    {
        //return (DateTime.Now - Program.StartDateTime).hum
        //return (DateTime.Now - Program.StartDateTime).Humanize(utcDate: false);
        return (DateTime.Now - MarsStartupInfo.StartDateTime).Humanize();
    }

    public DateTimeOffset AppStartDateTime()
    {
        return MarsStartupInfo.StartDateTime;
    }

    public IEnumerable<KeyValuePair<string, string>> HostCacheSettings()
    {
        var info = new List<KeyValuePair<string, string>>();

        var stat = _memoryCache.GetCurrentStatistics();

        Action<string, string> add = (k, v) => info.Add(new KeyValuePair<string, string>(k, v));

        add(nameof(stat.CurrentEntryCount), stat.CurrentEntryCount.ToString());
        add(nameof(stat.CurrentEstimatedSize), stat.CurrentEstimatedSize?.ToHumanizedSize() ?? "");
        add(nameof(stat.TotalHits), stat.TotalHits.ToString());
        add(nameof(stat.TotalMisses), stat.TotalMisses.ToString());

        return info;
    }

    public SystemMinStatResponse SystemMinStat()
    {
        return new SystemMinStatResponse
        {
            MemoryUsage = MemoryUsage(),
            GetMemoryUsageBytes = GetMemoryUsageBytes(),
            AppUptime = AppUptime(),
            AppStartDateTime = AppStartDateTime(),
            SystemUptime = SystemUptime(),
            SystemUptimeMillis = SystemUptimeMillis(),
        };
    }

    public string SystemUptime()
    {
        TimeSpan ts = TimeSpan.FromMilliseconds(Environment.TickCount64);
        return ts.Humanize(3, maxUnit: Humanizer.Localisation.TimeUnit.Day);
    }

    public long SystemUptimeMillis()
        => Environment.TickCount64;

    Debouncer memoryDebouncer = new Debouncer(1000);
    long _memoryUsage = 0;
    string _memoryUsageString = "0";

    void WriteMemoryUsage()
    {
        Process proc = Process.GetCurrentProcess();
        var mem = proc.PrivateMemorySize64;
        _memoryUsage = mem;
        _memoryUsageString = mem.ToHumanizedSize();
    }

    string GetMemoryUsage()
    {
        if (_memoryUsage == 0)
        {
            WriteMemoryUsage();
        }
        else
        {
            memoryDebouncer.Debouce(WriteMemoryUsage);
        }

        return _memoryUsageString;
    }

    long GetMemoryUsageBytes()
    {
        GetMemoryUsage();
        return _memoryUsage;
    }

    public string MemoryUsage()
        => GetMemoryUsage();

    async Task<AppTimeZones> GetTimeZone()
    {
        var connectionString = _optionService.GetDefaultDatabaseConnectionString();
        var databaseTimeZone = await _memoryCache.GetOrCreateAsync<TimeZoneInfo?>(
                                    nameof(AppTimeZones.DatabaseTimeZone),
                                    entry => entry.Value == null ?
                                        GetPostgresTimeZoneAsync(connectionString) :
                                        Task.FromResult((TimeZoneInfo?)entry.Value),
                                    new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromHours(1) });

        return new()
        {
            ServerTimeZone = TimeZoneInfo.Local,
            DatabaseTimeZone = databaseTimeZone
        };
    }

    async Task<string> GetPostgresTimeZoneStringAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Вариант 1: через SHOW timezone
        await using (var cmd = new NpgsqlCommand("SHOW timezone", connection))
        {
            var timeZone = (string)(await cmd.ExecuteScalarAsync())!;
            Console.WriteLine($"PostgreSQL TimeZone: {timeZone}");
            return timeZone;
        }

        // Вариант 2: через current_setting
        // await using (var cmd = new NpgsqlCommand("SELECT current_setting('TimeZone')", connection))
        // {
        //     return (string)await cmd.ExecuteScalarAsync();
        // }
    }

    async Task<TimeZoneInfo?> GetPostgresTimeZoneAsync(string connectionString)
    {
        try
        {
            var pgTimeZone = await GetPostgresTimeZoneStringAsync(connectionString);
            return TimeZoneInfo.FindSystemTimeZoneById(pgTimeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            MarsLogger.GetStaticLogger<MarsSystemService>().LogError("TimeZone not found, falling back to UTC");
            return null;
        }
    }

}

internal record AppTimeZones
{
    public required TimeZoneInfo ServerTimeZone { get; set; }
    public required TimeZoneInfo? DatabaseTimeZone { get; set; }
}
