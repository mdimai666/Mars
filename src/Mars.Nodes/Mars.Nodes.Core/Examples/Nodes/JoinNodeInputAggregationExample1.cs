using Mars.Nodes.Core.Nodes.Functions;
using Mars.Nodes.Core.Nodes.Sequences;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class JoinNodeInputAggregationExample1 : INodeExample<JoinNode>
{
    public string Name => "Aggregate by input port";
    public string Description => "";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        var builder = NodesWorkflowBuilder.Create()
            .AddNext(new InjectNode())
            .AddNext(
                    NodesWorkflowBuilder.Create()
                        .AddNext(new TemplateNode() { Name = "input 1", Template = "input 1" }),
                    NodesWorkflowBuilder.Create()
                        .AddNext(new DelayNode() { DelayMillis = 1000 })
                        .AddNext(new TemplateNode() { Name = "input 2", Template = "input 2" }),
                    NodesWorkflowBuilder.Create()
                        .AddNext(new DelayNode() { DelayMillis = 2000 })
                        .AddNext(new TemplateNode() { Name = "input 3", Template = "input 3" })
                    )
            .AddNext([new JoinNode() {
                Inputs = [new(), new(), new()],
                Mode = JoinNode.JoinMode.InputAggregation,
                InputAggregationTimeoutSeconds = 15
            }], catchAllWires: true)
            .AddNext(new DebugNode());

        var joinNode = builder.Nodes.First(node => node is JoinNode);
        var templates = builder.Nodes.Where(node => node is TemplateNode).ToList();
        for (int port = 0; port < templates.Count; port++)
        {
            var templateNode = templates[port];
            templateNode.Wires = [[new(joinNode.Id, port)]];
        }

        return builder.Build();
    }
}
