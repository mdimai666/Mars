using Mars.Core.Extensions;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core.Nodes;
using Microsoft.Extensions.Logging;

namespace Mars.Nodes.Core.Implements.Nodes;

#if DEBUG
/// <summary>
/// For experiments object
/// </summary>
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

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        //RED.DebugMsg(DebugMessage.NodeMessage(Node.Id, $"input port = {parameters.InputPort}"));
        Node.status = $"input port = {parameters.InputPort}";
        RED.Status(new NodeStatus(Node.status));

        var logger = MarsLogger.GetStaticLogger<DevMicroschemeNodeImpl>();

        if (parameters.InputPort == 0)
        {

            Tools.SetTimeout(() =>
            {
                //parameters.CancellationToken.ThrowIfCancellationRequested();
                logger.LogWarning("GOOD!");
                callback(input);
                RED.Done(parameters);
            }, 4000, parameters.CancellationToken);

            Tools.SetTimeout(() =>
            {
                logger.LogWarning("GOOD!");
                callback(input);
                RED.Done(parameters);
            }, 7000, parameters.CancellationToken);
        }
        else if (parameters.InputPort == 1)
        {
            foreach (var x in Enumerable.Range(0, 10))
            {
                parameters.CancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(500);
                input.Payload = $"{x + 1}/10";
                callback(input);
            }
            RED.Done(parameters);
        }
        else
        {
            RED.Done(parameters);
            throw new NotImplementedException();
        }
    }

}

#endif
