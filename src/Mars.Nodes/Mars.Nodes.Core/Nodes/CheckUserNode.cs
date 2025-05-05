using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/NodeFormEditor/Docs/CheckUserNode/CheckUserNode{.lang}.md")]
public class CheckUserNode : Node
{
    public CheckUserNode()
    {
        haveInput = true;
        Color = "#4cb5e6";
        Outputs = new List<NodeOutput> {
            new NodeOutput(){ Label = "Auth" },
            new NodeOutput(){ Label = "Non auth" },
        };
    }
}
