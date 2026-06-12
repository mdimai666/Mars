//#define DEBUG_TEMPLATOR_PERFOMANCE
using Mars.Core.Extensions;
using Mars.Core.Features;
using Mars.Host.Shared.TemplateEngine;
using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes;

public class TemplateNodeImpl : INodeImplement<TemplateNode>, INodeImplement, IDisposable
{
    public TemplateNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    private readonly ITemplateManager _templateManager;
    private bool _onceExecuted;

    public TemplateNodeImpl(TemplateNode node, IRED red, ITemplateManager templateManager)
    {
        Node = node;
        RED = red;
        _templateManager = templateManager;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        if (!_onceExecuted) _onceExecuted = true;

        var data = input.AsFullDict();
        var key = "node-" + Node.Id;
        var render = _templateManager.RenderCached(Node.TemplateEngineId, key, Node.Template, data);

#if DEBUG_TEMPLATOR_PERFOMANCE
        RED.Status(new() { Text = $"ts: {TimeSpanParser.Format(render.Elapsed)}, allocated: {render.AllocatedBytes.ToHumanizedSize()}" });
#endif

        if (Node.Property == "Payload")
            input.Payload = render.Content;
        else
            input.Set(Node.Property, render.Content);

        callback(input);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_onceExecuted)
        {
            var key = "node-" + Node.Id;
            _templateManager.ClearCacheForEngineItem(Node.TemplateEngineId, key);
        }
    }
}
