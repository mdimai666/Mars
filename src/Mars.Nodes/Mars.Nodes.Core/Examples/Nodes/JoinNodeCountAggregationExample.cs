using Mars.Nodes.Core.Nodes.Functions;
using Mars.Nodes.Core.Nodes.Sequences;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class JoinNodeCountAggregationExample : INodeExample<JoinNode>
{
    public string Name => "Aggregate by count";
    public string Description => "";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        ForeachNode foreachNode;
        TemplateNode templateNode;

        var builder = NodesWorkflowBuilder.Create()
            .AddNext(new InjectNode() { Payload = "123456789", Name = "123456789" })
            .AddNext(foreachNode = new ForeachNode())
            .AddNext(templateNode = new TemplateNode() { Name = "item", Template = "item: {{Payload}}" })
            .AddNext(new JoinNode()
            {
                Inputs = [new()],
                Mode = JoinNode.JoinMode.CountAggregation,
                MessageCount = 3
            })
            .AddNext(new DebugNode());

        foreachNode.Wires = [[], [templateNode.Id]];
        templateNode.Wires = [[.. templateNode.Wires[0], new(foreachNode.Id, 1)]];
        builder.BuilderItems[templateNode.Id].ElementRowIndex = 2;

        return builder.Build();
    }
}
