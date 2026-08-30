using Mars.Nodes.Abstractions;
using Mars.Nodes.Abstractions.Services;
using Mars.Nodes.Core.Nodes.TaskNodes;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Core.Implements.Nodes.TaskNodes;

public class TerminateAllJobsNodeImpl : INodeImplement<TerminateAllJobsNode>
{

    public TerminateAllJobsNode Node { get; }
    Node INodeImplement.Node => Node;
    public IRuntimeNodeScope RNS { get; set; }

    public TerminateAllJobsNodeImpl(TerminateAllJobsNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        var nodeTaskManager = RNS.ServiceProvider.GetRequiredService<INodeTaskManager>();

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
