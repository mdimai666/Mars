using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class VariableSetNodeIncrementVariableExample : INodeExample<VariableSetNode>
{
    public string Name => "Increment variable";
    public string Description => "Increment variable";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        var nodes = NodesWorkflowBuilder.Create()
            .AddNext(new InjectNode())
            .AddNext(new VariableSetNode()
            {
                Setters = [
                    new VariableSetExpression { ValuePath = "VarNode.xx", Expression = "VarNode.xx+1", Operation = VariableSetOperation.Set },
                    new VariableSetExpression { ValuePath = "msg.Payload", Expression = "VarNode.xx", Operation = VariableSetOperation.Set }
                ]
            })
            .AddNext(new DebugNode())
            .Build();

        var xxNodeExist = editorState.Nodes.Any(node => node is VarNode && node.Name == "xx");

        return xxNodeExist ? nodes : [.. nodes, new VarNode { Name = "xx", VarType = "int", DefaultValue = "0", Value = 0 }];
    }
}
