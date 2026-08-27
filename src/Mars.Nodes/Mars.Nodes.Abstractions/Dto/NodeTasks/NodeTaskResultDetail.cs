namespace Mars.Nodes.Abstractions.Dto.NodeTasks;

public record NodeTaskResultDetail : NodeTaskResultSummary
{
    public required IReadOnlyCollection<NodeJobDto> Jobs { get; init; }
}
