using System.Diagnostics;
using Mars.Core.Extensions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Exceptions;
using Mars.Nodes.Host.Shared;
using Mars.SemanticKernel.Host.Service;
using Mars.SemanticKernel.Shared.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.SemanticKernel.Host.Nodes;

public class AIRequestNodeImpl : INodeImplement<AIRequestNode>
{
    public AIRequestNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;
    public AIRequestNodeImpl(AIRequestNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;

        Node.Config = RNS.GetConfig(node.Config);
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        if (Node.Config.Value is null)
        {
            RNS.Status(new NodeStatus("not configured"));
            throw new NodeExecuteException(Node, "model not configured");
        }

        var sw = new Stopwatch();
        sw.Start();
        RNS.Status(new NodeStatus("think.."));

        try
        {
            var userPrompt = Node.Prompt.AsNullIfEmptyOrWhiteSpace() ?? input.Payload?.ToString()!;

            var aiHandler = RNS.ServiceProvider.GetRequiredService<NodesAiRequestHandler>();

            var reply = await aiHandler.Handle(userPrompt, Node.Config.Value, parameters.CancellationToken);

            input.Payload = reply.Content;

            callback(input);
        }
        catch (Exception ex)
        {
            RNS.DebugMsg(ex);
        }

        sw.Stop();
        var totalTime = sw.ElapsedMilliseconds > 1000 ? $"{sw.Elapsed.TotalSeconds:0.0}s" : $"{sw.ElapsedMilliseconds / 1000:0.00}ms";
        RNS.Status(new NodeStatus(totalTime));
    }

}
