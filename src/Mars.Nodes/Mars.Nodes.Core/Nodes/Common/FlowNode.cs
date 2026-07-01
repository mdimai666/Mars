using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes.Common;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/FlowNode/FlowNode{.lang}.md")]
public class FlowNode : Node
{
    public override string TypeId => "core.FlowNode";

    public int Order { get; set; }
}
