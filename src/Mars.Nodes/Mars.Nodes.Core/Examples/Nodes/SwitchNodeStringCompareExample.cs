using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class SwitchNodeStringCompareExample : INodeExample<SwitchNode>
{
    public string Name => "string equal condition";
    public string Description => "string equal condition";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        var builder = NodesWorkflowBuilder.Create()
            .AddNext(new InjectNode() { Payload = "good", Name = "good" })
            .AddNext(new SwitchNode()
            {
                BreakAfterFirst = false,
                Conditions = [  new SwitchNode.Condition{ Value = "msg.Payload != \"good\"" },
                                new SwitchNode.Condition{ Value = "msg.Payload == \"good\"" },
                                new SwitchNode.Condition{ Value = "msg.Payload.ToString().Length == 4" }],
                Outputs = [new(), new(), new()]
            })
            .AddNext(new DebugNode(), new DebugNode(), new DebugNode());

        var switchNode = builder.Nodes.First(node => node is SwitchNode);
        var debugNodes = builder.Nodes.Where(node => node is DebugNode).ToList();
        switchNode.Wires = [[debugNodes[0].Id], [debugNodes[1].Id], [debugNodes[2].Id]];

        return builder.Build();
    }
}
