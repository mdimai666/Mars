using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class JoinNodeTimeAggregationExample : INodeExample<JoinNode>
{
    public string Name => "Aggregate by time";
    public string Description => "";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        ForeachNode foreachNode;
        TemplateNode templateNode;
        DelayNode delayNode;

        var builder = NodesWorkflowBuilder.Create()
            .AddNext(new InjectNode() { Payload = "123456789", Name = "123456789" })
            .AddNext(foreachNode = new ForeachNode())
            .AddNext(templateNode = new TemplateNode() { Name = "item", Template = "item: {{Payload}}" })
            .AddNext(delayNode = new DelayNode() { DelayMillis = 500 })
            .AddNext(new JoinNode()
            {
                Inputs = [new()],
                Mode = JoinNode.JoinMode.TimeAggregation,
                AggregationTimeSeconds = 2
            })
            .AddNext(new DebugNode());

        foreachNode.Wires = [[], [templateNode.Id]];
        delayNode.Wires = [[.. delayNode.Wires[0], new(foreachNode.Id, 1)]];
        builder.BuilderItems[templateNode.Id].ElementRowIndex = 2;
        builder.BuilderItems[delayNode.Id].ElementRowIndex = 2;

        return builder.Build();
    }
}
