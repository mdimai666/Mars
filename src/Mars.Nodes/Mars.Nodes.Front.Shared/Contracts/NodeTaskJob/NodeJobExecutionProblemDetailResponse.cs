namespace Mars.Nodes.Front.Shared.Contracts.NodeTaskJob;

public record NodeJobExecutionProblemDetailResponse
{
    public required string Message { get; init; }
    public required string? StackTrace { get; init; }

}
