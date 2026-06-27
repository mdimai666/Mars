using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.Shared;
using Mars.Nodes.Host.Shared.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Core.Implements.Nodes;

public class KillTaskJobNodeImpl : INodeImplement<KillTaskJobNode>
{

    public KillTaskJobNode Node { get; }
    Node INodeImplement.Node => Node;
    public IRuntimeNodeScope RNS { get; set; }

    public KillTaskJobNodeImpl(KillTaskJobNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        var nodeTaskManager = RNS.ServiceProvider.GetRequiredService<INodeTaskManager>();

        nodeTaskManager.TryKillTaskJob(parameters.TaskId);
        RNS.DebugMsg(DebugMessage.NodeWarnMessage(Node.Id, "Task killed"));

        CreateNewTaskForNextWires(this);

        return Task.CompletedTask;
    }

    public static void CreateNewTaskForNextWires(INodeImplement nodeImpl, NodeMsg? msg = null, int outputPortIndex = 0)
    {
        var nodeTaskManager = nodeImpl.RNS.ServiceProvider.GetRequiredService<INodeTaskManager>();
        var node = nodeImpl.Node;

        var outputNodes = node.Wires.ElementAt(outputPortIndex);
        if (outputNodes.Any())
        {
            foreach (var outputNode in outputNodes)
            {
                nodeTaskManager.CreateJob(nodeImpl.RNS.ServiceProvider, outputNode.NodeId, msg);
            }
        }
    }

    public static void CreateNewTask(INodeImplement nodeImpl, string nodeId, NodeMsg? msg = null)
    {
        var nodeTaskManager = nodeImpl.RNS.ServiceProvider.GetRequiredService<INodeTaskManager>();

        nodeTaskManager.CreateJob(nodeImpl.RNS.ServiceProvider, nodeId, msg);
    }
}
