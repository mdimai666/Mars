using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/DelayNode/DelayNode{.lang}.md")]
public class DelayNode : Node
{
    public int DelayMillis { get; set; } = 1000;

    public DelayNode()
    {
        HaveInput = true;
        Color = "#e6e0f8";
        Outputs = new List<NodeOutput> {
            new NodeOutput(){ Label = "iterate" },
        };
        Icon = "_content/Mars.Nodes.Workspace/nodes/hourglass-split.svg";
    }
}
