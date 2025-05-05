namespace Mars.Shared.Contracts.Systems;

public record SystemMinStatResponse
{
    public required string MemoryUsage { get; init; }
    public required long GetMemoryUsageBytes { get; init; }
    public required string AppUptime { get; init; }
    public required DateTimeOffset AppStartDateTime { get; init; }
    public required string SystemUptime { get; init; }
    public required long SystemUptimeMillis { get; init; }

    public static SystemMinStatResponse Empty()
        => new()
        {
            MemoryUsage = "",
            GetMemoryUsageBytes = 0,
            AppUptime = "",
            AppStartDateTime = DateTimeOffset.MinValue,
            SystemUptime = "",
            SystemUptimeMillis = 0
        };
}
