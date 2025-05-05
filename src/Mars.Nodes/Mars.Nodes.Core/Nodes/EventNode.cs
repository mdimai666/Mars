using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/NodeFormEditor/Docs/EventNode/EventNode{.lang}.md")]
public class EventNode : Node
{
    public string Topics { get; set; } = "";

    public EventNode()
    {
        haveInput = false;
        Color = "#e6e0f8";
        Outputs = new List<NodeOutput> { new NodeOutput() };
        //Icon = "";
    }
}
