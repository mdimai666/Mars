using Mars.Core.Extensions;
using Mars.Core.Features;
using Mars.Host.Shared.TemplateEngine;
using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes;

public class TemplateNodeImpl : INodeImplement<TemplateNode>, INodeImplement
{
    public TemplateNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    private readonly ITemplateManager _templateManager;

    public TemplateNodeImpl(TemplateNode node, IRED red, ITemplateManager templateManager)
    {
        Node = node;
        RED = red;
        _templateManager = templateManager;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        var data = input.AsFullDict();

        var render = _templateManager.RenderCached(Node.TemplateEngineId, "node-" + Node.Id, Node.Template, data);

#if DEBUG
        RED.Status(new() { Text = $"ts: {TimeSpanParser.Format(render.Elapsed)}, allocated: {render.AllocatedBytes.ToHumanizedSize()}" });
#endif

        if (Node.Property == "Payload")
            input = input.Copy(render.Content);
        else
            input.Set(Node.Property, render.Content);

        callback(input);

        return Task.CompletedTask;
    }
}
