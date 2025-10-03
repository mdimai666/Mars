using Mars.Nodes.Core.Implements;

namespace Mars.Nodes.Host.NodeTasks;

internal class NodeJob
{
    public INodeImplement NodeImplement { get; init; }
    public List<NodeJobExecutionTime> Executions { get; set; } = [];
    public bool IsDone => Executions.All(s => s.Result is not NodeJobExecutionResult.None and not NodeJobExecutionResult.Pending);

    public NodeJob(INodeImplement nodeImplement)
    {
        NodeImplement = nodeImplement;
    }

    public NodeJobExecutionTime CreateExecutionStart()
    {
        var e = Executions;
        var et = new NodeJobExecutionTime() { Start = DateTimeOffset.Now };
        e.Add(et);
        return et;
    }
}
