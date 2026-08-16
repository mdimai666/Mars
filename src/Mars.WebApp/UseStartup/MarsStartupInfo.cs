using System.Reflection;
using Mars.Host.Shared.Startup;

namespace Mars.UseStartup;

public static class MarsStartupInfo
{
    public static readonly DateTimeOffset StartDateTime = DateTimeOffset.Now;
    public static readonly string StartWorkDirectory = Environment.CurrentDirectory;

    public static readonly string ASPNETCORE_ENVIRONMENT = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "";
    public static readonly bool IsDevelopment;
    public static readonly bool IsTesting;
    public static readonly bool IsRunningInDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
    public static readonly bool IsRunUnderVisualStudio = Environment.GetEnvironmentVariable("VisualStudioEdition") is not null;
    public static readonly string Version;

    static MarsStartupInfo()
    {
        IsDevelopment = ASPNETCORE_ENVIRONMENT.Equals("Development", StringComparison.OrdinalIgnoreCase);
        IsTesting = ASPNETCORE_ENVIRONMENT.Equals("Test", StringComparison.OrdinalIgnoreCase);

        var assembly = Assembly.GetExecutingAssembly();
        Version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
    }

    public static IMarsStartupInfo Instance = new MarsStartupInfoObject();
}

internal class MarsStartupInfoObject : IMarsStartupInfo
{
    public DateTimeOffset StartDateTime => MarsStartupInfo.StartDateTime;
    public string StartWorkDirectory => MarsStartupInfo.StartWorkDirectory;

    public string ASPNETCORE_ENVIRONMENT => MarsStartupInfo.ASPNETCORE_ENVIRONMENT;
    public bool IsDevelopment => MarsStartupInfo.IsDevelopment;
    public bool IsTesting => MarsStartupInfo.IsTesting;
    public bool IsRunningInDocker => MarsStartupInfo.IsRunningInDocker;
    public bool IsRunUnderVisualStudio => MarsStartupInfo.IsRunUnderVisualStudio;
    public string Version => MarsStartupInfo.Version;
}
