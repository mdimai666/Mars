namespace Mars.Nodes.Core.Dto.NodeTasks;

public record NodeJobExecutionProblemDetailDto
{
    public string Message { get; init; } = string.Empty;
    public string? StackTrace { get; init; }

    public NodeJobExecutionProblemDetailDto()
    {

    }

    public NodeJobExecutionProblemDetailDto(Exception exception)
    {
        Message = exception.Message;
        StackTrace = exception.StackTrace;
    }
}
