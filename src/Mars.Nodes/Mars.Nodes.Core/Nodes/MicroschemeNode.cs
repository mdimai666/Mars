using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/MicroschemeNode/MicroschemeNode{.lang}.md")]
public class MicroschemeNode : Node
{
    public override string Label => "Micr";

    public MicroschemeNode()
    {
        isInjectable = true;
        Color = "#A9BBCF";
        Outputs = new List<NodeOutput> { new NodeOutput() };
    }
}
