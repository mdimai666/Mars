using Mars.Core.Extensions;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core.Nodes;
using Microsoft.Extensions.Logging;

namespace Mars.Nodes.Core.Implements.Nodes;

#if DEBUG
public class DevMicroschemeNodeImpl : INodeImplement<DevMicroschemeNode>, INodeImplement, ISelfFinalizingNode
{
    public DevMicroschemeNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public DevMicroschemeNodeImpl(DevMicroschemeNode node, IRED red)
    {
        Node = node;
        RED = red;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        //RED.DebugMsg(DebugMessage.NodeMessage(Node.Id, $"input port = {parameters.InputPort}"));
        Node.status = $"input port = {parameters.InputPort}";
        RED.Status(new NodeStatus(Node.status));

        callback(input);

        var logger = MarsLogger.GetStaticLogger<DevMicroschemeNodeImpl>();

        Tools.SetTimeout(() =>
        {
            logger.LogWarning("GOOD!");
            RED.Done(parameters);
        }, 1000);

        return Task.CompletedTask;
    }

}

#endif
