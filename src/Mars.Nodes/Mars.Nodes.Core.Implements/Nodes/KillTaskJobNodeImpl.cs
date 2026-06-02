using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.Shared.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Core.Implements.Nodes;

public class KillTaskJobNodeImpl : INodeImplement<KillTaskJobNode>, INodeImplement
{

    public KillTaskJobNode Node { get; }
    Node INodeImplement<Node>.Node => Node;
    public IRED RED { get; set; }

    public KillTaskJobNodeImpl(KillTaskJobNode node, IRED red)
    {
        Node = node;
        RED = red;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        var nodeTaskManager = RED.ServiceProvider.GetRequiredService<INodeTaskManager>();

        nodeTaskManager.TryKillTaskJob(parameters.TaskId);
        RED.DebugMsg(DebugMessage.NodeWarnMessage(Node.Id, "Task killed"));

        CreateNewTaskForNextWires(this);

        return Task.CompletedTask;
    }

    public static void CreateNewTaskForNextWires(INodeImplement nodeImpl, NodeMsg? msg = null, int outputPortIndex = 0)
    {
        var nodeTaskManager = nodeImpl.RED.ServiceProvider.GetRequiredService<INodeTaskManager>();
        var node = nodeImpl.Node;

        var outputNodes = node.Wires.ElementAt(outputPortIndex);
        if (outputNodes.Any())
        {
            foreach (var outputNode in outputNodes)
            {
                nodeTaskManager.CreateJob(nodeImpl.RED.ServiceProvider, outputNode.NodeId, msg);
            }
        }
    }

    public static void CreateNewTask(INodeImplement nodeImpl, string nodeId, NodeMsg? msg = null)
    {
        var nodeTaskManager = nodeImpl.RED.ServiceProvider.GetRequiredService<INodeTaskManager>();

        nodeTaskManager.CreateJob(nodeImpl.RED.ServiceProvider, nodeId, msg);
    }
}
