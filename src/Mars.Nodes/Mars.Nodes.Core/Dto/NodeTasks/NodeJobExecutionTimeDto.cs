namespace Mars.Nodes.Core.Dto.NodeTasks;

public record NodeJobExecutionTimeDto
{
    public required Guid JobGuid { get; init; }
    public required DateTimeOffset Start { get; init; }
    public required DateTimeOffset? End { get; init; }
    public required NodeJobExecutionResultDto Result { get; init; }
    public required NodeJobExecutionProblemDetailDto? Exception { get; init; }
}
