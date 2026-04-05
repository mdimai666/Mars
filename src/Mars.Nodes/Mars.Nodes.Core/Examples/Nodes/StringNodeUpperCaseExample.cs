using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.StringFunctions;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class StringNodeUpperCaseExample : INodeExample<StringNode>
{
    public string Name => "Uppercase input";
    public string Description => "Uppercase input";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        return NodesWorkflowBuilder.Create()
            .AddNext(new InjectNode() { Payload = "text", Name = "text" })
            .AddNext(new StringNode() { Operations = [new() { Method = nameof(StringNodeOperationUtils.ToUpper) }] })
            .AddNext(new DebugNode())
            .Build();
    }
}
