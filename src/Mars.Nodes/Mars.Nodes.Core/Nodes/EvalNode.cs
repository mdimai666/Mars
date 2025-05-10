using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/EvalNode/EvalNode{.lang}.md")]
public class EvalNode : Node
{
    public string Input { get; set; } = "Payload + 1";

    public EvalNode()
    {
        haveInput = true;
        Color = "#d4eba1";
        Outputs = new List<NodeOutput> { new NodeOutput() };
    }

}
