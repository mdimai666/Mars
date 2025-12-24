namespace Mars.Nodes.Front.Shared.Contracts.NodeTaskJob;

public record NodeTaskResultSummaryResponse
{
    public required Guid TaskId { get; init; }
    public required int ExecuteCount { get; init; }
    public required int NodesChainCount { get; init; }
    public required string InjectNodeId { get; init; }
    public required string FlowNodeId { get; init; }
    public required bool IsDone { get; init; }
    public required bool IsTerminated { get; init; }
    public required int ErrorCount { get; init; }
    public required DateTimeOffset StartDate { get; init; }
    public required DateTimeOffset? EndDate { get; init; }

    public required string FlowName { get; init; }
    public required string InjectNodeDisplayName { get; init; }

}
