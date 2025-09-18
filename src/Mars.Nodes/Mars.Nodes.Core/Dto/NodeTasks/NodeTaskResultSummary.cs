namespace Mars.Nodes.Core.Dto.NodeTasks;

public record NodeTaskResultSummary
{
    public required Guid TaskId { get; init; }
    public required int ExecuteCount { get; init; }
    public required int NodesChainCount { get; init; }
    public required string InjectNodeId { get; init; }
    public required string FlowNodeId { get; init; }
    public required bool IsDone { get; init; }
    public required bool IsTerminated { get; init; }
    public required int ErrorCount { get; init; }
}
