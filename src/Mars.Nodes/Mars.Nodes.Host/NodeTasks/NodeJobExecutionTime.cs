namespace Mars.Nodes.Host.NodeTasks;

internal class NodeJobExecutionTime
{
    public Guid JobGuid { get; } = Guid.NewGuid();
    public DateTimeOffset Start { get; init; }
    public DateTimeOffset? End { get; private set; }
    public NodeJobExecutionResult Result { get; private set; }
    public Exception? Exception { get; set; }

    public void Success()
    {
        End = DateTimeOffset.Now;
        Result = NodeJobExecutionResult.Success;
    }

    public void Fail(Exception exception)
    {
        End = DateTimeOffset.Now;
        Exception = exception;
        Result = NodeJobExecutionResult.Fail;
    }

    public void Pending()
    {
        Result = NodeJobExecutionResult.Pending;
    }
}
