using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class InjectNodeMultipleFieldsExample1 : INodeExample<InjectNode>
{
    public string Name => "Multiple fields";
    public string Description => "Inject Payload, status and timestamp fields into the message.";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        return NodesWorkflowBuilder.Create()
            .AddNext(new InjectNode
            {
                Fields =
                [
                    new() { Key = "Payload", VarType = "string", Value = "Hello from Inject!" },
                    new() { Key = "status", VarType = "string", Value = "ok" },
                    new() { Key = "timestamp", VarType = VarNode.TimestampTypeName },
                ]
            })
            .AddNext(new DebugNode())
            .Build();
    }
}
