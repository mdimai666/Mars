using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/NodeFormEditor/Docs/DelayNode/DelayNode{.lang}.md")]
public class DelayNode : Node
{
    public int DelayMillis { get; set; } = 1000;

    public DelayNode()
    {
        haveInput = true;
        Color = "#e6e0f8";
        Outputs = new List<NodeOutput> {
            new NodeOutput(){ Label = "iterate" },
        };
    }
}
