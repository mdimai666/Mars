using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class SwitchNodeCalculatedComparerExample : INodeExample<SwitchNode>
{
    public string Name => "Calculated condition input";
    public string Description => "Calculated condition input";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        var builder = NodesWorkflowBuilder.Create()
            .AddNext(new InjectNode())
            .AddNext(new EvalNode() { Input = "6" })
            .AddNext(new SwitchNode()
            {
                BreakAfterFirst = true,
                Conditions = [  new SwitchNode.Condition{ Value = "msg.Payload * 2 > 10" },
                                new SwitchNode.Condition{ Value = "true" }],
                Outputs = [new(), new()]
            })
            .AddNext(new DebugNode(), new DebugNode());

        var switchNode = builder.Nodes.First(node => node is SwitchNode);
        var debugNodes = builder.Nodes.Where(node => node is DebugNode).ToList();
        switchNode.Wires = [[debugNodes[0].Id], [debugNodes[1].Id]];

        return builder.Build();
    }
}
