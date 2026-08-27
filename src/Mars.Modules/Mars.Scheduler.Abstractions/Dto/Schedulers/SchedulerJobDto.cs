namespace Mars.Host.Shared.Dto.Schedulers;

public class SchedulerJobDto
{
    public IReadOnlyCollection<SchedulerMaskDto> Triggers { get; init; } = default!;
    public DateTimeOffset? NextExecutionTime { get; init; }
    public string Name { get; init; } = default!;
    public string Group { get; init; } = default!;

}

public class SchedulerMaskDto
{
    public bool IsCron { get; init; } = default!;
    public string Mask { get; init; } = default!;
    public string Description { get; init; } = default!;
    /// <summary>
    /// see Quartz.TriggerState
    /// </summary>
    public string TriggerState { get; init; } = default!;
}
