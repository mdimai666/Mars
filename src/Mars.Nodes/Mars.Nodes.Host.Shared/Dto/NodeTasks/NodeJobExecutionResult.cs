namespace Mars.Nodes.Host.Shared.Dto.NodeTasks;

public enum NodeJobExecutionResult : int
{
    None = 0,
    Success = 1,
    Pending = 2,
    Fail = -1,
}
