namespace Mars.Nodes.Core.Dto.NodeTasks;

public record NodeTaskResultDetail : NodeTaskResultSummary
{
    public required IReadOnlyCollection<NodeJobDto> Jobs { get; init; }
}
