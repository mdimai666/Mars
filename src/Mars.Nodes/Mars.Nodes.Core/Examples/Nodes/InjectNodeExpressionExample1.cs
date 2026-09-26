using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class InjectNodeExpressionExample1 : INodeExample<InjectNode>
{
    public string Name => "Expression fields";
    public string Description => "Inject fields computed by C# expressions.";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        return NodesWorkflowBuilder.Create()
            .AddNext(new InjectNode
            {
                Fields =
                [
                    new() { Key = "Payload", VarType = "int", ValueKind = InputValueKind.Expression, Value = "21 * 2" },
                    new() { Key = "label", VarType = "string", ValueKind = InputValueKind.Expression, Value = "\"answer=\" + (21 * 2)" },
                ]
            })
            .AddNext(new DebugNode())
            .Build();
    }
}
