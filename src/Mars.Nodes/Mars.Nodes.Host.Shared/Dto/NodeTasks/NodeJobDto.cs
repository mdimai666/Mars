namespace Mars.Nodes.Host.Shared.Dto.NodeTasks;

public record NodeJobDto
{
    public required string NodeId { get; init; }
    public required IReadOnlyCollection<NodeJobExecutionTimeDto> Executions { get; init; }
}
