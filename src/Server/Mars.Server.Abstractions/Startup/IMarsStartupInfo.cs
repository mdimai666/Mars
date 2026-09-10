namespace Mars.Server.Abstractions.Startup;

public interface IMarsStartupInfo
{
    public DateTimeOffset StartDateTime { get; }
    public string StartWorkDirectory { get; }

    public string ASPNETCORE_ENVIRONMENT { get; }
    public bool IsDevelopment { get; }
    public bool IsTesting { get; }
    public bool IsRunningInDocker { get; }
    public bool IsRunUnderVisualStudio { get; }
    public string Version { get; }
}
