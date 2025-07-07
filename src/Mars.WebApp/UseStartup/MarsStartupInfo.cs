using System.Reflection;

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

}
