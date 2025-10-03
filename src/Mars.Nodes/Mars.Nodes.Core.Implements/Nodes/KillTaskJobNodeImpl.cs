using Mars.Host.Shared.Services;
using Mars.Nodes.Core.Nodes;
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

        var outputNodes = Node.Wires.First();
        if (outputNodes.Any())
        {
            foreach (var outputNode in outputNodes)
            {
                nodeTaskManager.CreateJob(RED.ServiceProvider, outputNode.NodeId, new());
            }
        }

        return Task.CompletedTask;
    }
}
