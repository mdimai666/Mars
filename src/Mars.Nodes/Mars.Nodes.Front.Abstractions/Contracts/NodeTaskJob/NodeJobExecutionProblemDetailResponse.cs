namespace Mars.Nodes.Front.Abstractions.Contracts.NodeTaskJob;

public record NodeJobExecutionProblemDetailResponse
{
    public required string Message { get; init; }
    public required string? StackTrace { get; init; }
    public required string? ExceptionType { get; init; }

}
