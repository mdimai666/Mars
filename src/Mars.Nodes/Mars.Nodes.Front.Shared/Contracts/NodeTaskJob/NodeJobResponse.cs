namespace Mars.Nodes.Front.Shared.Contracts.NodeTaskJob;

public record NodeJobResponse
{
    public required string NodeId { get; init; }
    public required IReadOnlyCollection<NodeJobExecutionTimeResponse> Executions { get; init; }
}
