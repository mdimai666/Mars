using System.Diagnostics;
using System.Reflection;
using Humanizer;
using Mars.Core.Extensions;
using Mars.Host.Shared.Services;
using Mars.Shared.Contracts.Systems;
using Mars.UseStartup;
using Microsoft.Extensions.Caching.Memory;

namespace Mars.Services;

internal class MarsSystemService : IMarsSystemService
{
    private readonly IMemoryCache _memoryCache;

    static List<KeyValuePair<string, string>>? cachedAboutSystem;

    public MarsSystemService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
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
}
