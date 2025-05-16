using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/EventNode/EventNode{.lang}.md")]
public class EventNode : Node
{
    public string Topics { get; set; } = "";

    public EventNode()
    {
        HaveInput = false;
        Color = "#e6e0f8";
        Outputs = new List<NodeOutput> { new NodeOutput() };
        //Icon = "";
    }
}
