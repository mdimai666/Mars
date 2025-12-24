using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.Shared.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Core.Implements.Nodes;

public class TerminateAllJobsNodeImpl : INodeImplement<TerminateAllJobsNode>, INodeImplement
{

    public TerminateAllJobsNode Node { get; }
    Node INodeImplement<Node>.Node => Node;
    public IRED RED { get; set; }

    public TerminateAllJobsNodeImpl(TerminateAllJobsNode node, IRED red)
    {
        Node = node;
        RED = red;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        var nodeTaskManager = RED.ServiceProvider.GetRequiredService<INodeTaskManager>();

        if (Node.Scope == TerminateNodesScope.All)
        {
            nodeTaskManager.TerminateAllJobs();
        }
        else if (Node.Scope == TerminateNodesScope.Flow)
        {
            foreach (var task in nodeTaskManager.CurrentTasks().Where(s => s.FlowNodeId == Node.Container).ToList())
            {
                nodeTaskManager.TryKillTaskJob(task.TaskId);
            }
        }
        else
        {
            throw new NotImplementedException($"Terminate '{Node.Scope}' not implement");
        }

        return Task.CompletedTask;
    }
}
