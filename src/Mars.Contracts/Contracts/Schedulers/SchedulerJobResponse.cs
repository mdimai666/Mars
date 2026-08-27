namespace Mars.Shared.Contracts.Schedulers;

public record SchedulerJobResponse
{
    public required IReadOnlyCollection<SchedulerMaskResponse> Triggers { get; init; }
    public required DateTimeOffset? NextExecutionTime { get; init; }
    public required string Name { get; init; }
    public required string Group { get; init; }

}

public record SchedulerMaskResponse
{
    public required bool IsCron { get; init; }
    public required string Mask { get; init; }
    public required string Description { get; init; }
    /// <summary>
    /// see Quartz.TriggerState
    /// </summary>
    public required string TriggerState { get; init; }
}
