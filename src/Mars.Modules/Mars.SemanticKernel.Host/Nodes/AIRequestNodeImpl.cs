using System.Diagnostics;
using Mars.Core.Extensions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Exceptions;
using Mars.Nodes.Core.Implements;
using Mars.SemanticKernel.Host.Shared.Interfaces;
using Mars.SemanticKernel.Shared.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Ollama;

namespace Mars.SemanticKernel.Host.Nodes;

public class AIRequestNodeImpl : INodeImplement<AIRequestNode>, INodeImplement
{
    public AIRequestNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;
    public AIRequestNodeImpl(AIRequestNode node, IRED red)
    {
        Node = node;
        RED = red;

        Node.Config = RED.GetConfig(node.Config);
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        if (Node.Config.Value is null)
        {
            RED.Status(new NodeStatus("not configured"));
            throw new NodeExecuteException(Node, "model not configured");
        }

        var sw = new Stopwatch();
        sw.Start();
        RED.Status(new NodeStatus("think.."));

        try
        {
            var executionSettings = GetExecutionSettings();

            var systemPrompt = Node.Config.Value.SystemPrompt;
            var userPrompt = Node.Prompt.AsNullIfEmptyOrWhiteSpace() ?? input.Payload?.ToString()!;

            var ai = RED.ServiceProvider.GetRequiredService<IMarsAIService>();

            var reply = await ai.Reply(userPrompt, systemPrompt, Node.Config.Value, executionSettings);

            input.Payload = reply;

            callback(input);
        }
        catch (Exception ex)
        {
            RED.DebugMsg(ex);
        }

        sw.Stop();
        var totalTime = sw.ElapsedMilliseconds > 1000 ? $"{sw.Elapsed.TotalSeconds:0.0}s" : $"{sw.ElapsedMilliseconds / 1000:0.00}ms";
        RED.Status(new NodeStatus(totalTime));
    }

    PromptExecutionSettings GetExecutionSettings()
    {
        var executionSettings = new OllamaPromptExecutionSettings
        {
            Temperature = Node.Temperature ?? Node.Config.Value.Temperature,
            TopK = Node.TopK ?? Node.Config.Value.TopK,
            TopP = Node.TopP ?? Node.Config.Value.TopP,
        };

        return executionSettings;
    }
}
