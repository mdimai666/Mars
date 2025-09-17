namespace Mars.Nodes.Host.NodeTasks;

internal enum NodeJobExecutionResult : int
{
    None = 0,
    Success = 1,
    Pending = 2,
    Fail = -1,
}
