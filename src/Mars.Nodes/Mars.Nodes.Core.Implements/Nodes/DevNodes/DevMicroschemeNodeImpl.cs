using Mars.Core.Extensions;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core.Nodes.DevNodes;
using Mars.Nodes.Host.Shared;
using Microsoft.Extensions.Logging;

namespace Mars.Nodes.Core.Implements.Nodes.DevNodes;

#if DEBUG
/// <summary>
/// For experiments object
/// </summary>
public class DevMicroschemeNodeImpl : INodeImplement<DevMicroschemeNode>, ISelfFinalizingNode
{
    public DevMicroschemeNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public DevMicroschemeNodeImpl(DevMicroschemeNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        //RNS.DebugMsg(DebugMessage.NodeMessage(Node.Id, $"input port = {parameters.InputPort}"));
        Node.status = $"input port = {parameters.InputPort}";
        RNS.Status(new NodeStatus(Node.status));

        var logger = MarsLogger.GetStaticLogger<DevMicroschemeNodeImpl>();

        if (parameters.InputPort == 0)
        {

            Tools.SetTimeout(() =>
            {
                //parameters.CancellationToken.ThrowIfCancellationRequested();
                logger.LogWarning("GOOD!");
                callback(input);
                RNS.Done(parameters);
            }, 4000, parameters.CancellationToken);

            Tools.SetTimeout(() =>
            {
                logger.LogWarning("GOOD!");
                callback(input);
                RNS.Done(parameters);
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
            RNS.Done(parameters);
        }
        else
        {
            RNS.Done(parameters);
            throw new NotImplementedException();
        }
    }

}

#endif
