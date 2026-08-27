namespace Mars.Nodes.Host.Shared.Dto.NodeTasks;

public record NodeTaskResultDetail : NodeTaskResultSummary
{
    public required IReadOnlyCollection<NodeJobDto> Jobs { get; init; }
}
