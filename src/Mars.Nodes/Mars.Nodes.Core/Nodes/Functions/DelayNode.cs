using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes.Functions;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/DelayNode/DelayNode{.lang}.md")]
[Display(GroupName = "function")]
public class DelayNode : Node
{
    public override string TypeId => "core.DelayNode";

    public int DelayMillis { get; set; } = 1000;

    public DelayNode()
    {
        Inputs = [new()];
        Color = "#e6e0f8";
        Outputs = [new()];
        Icon = "_content/Mars.Nodes.Workspace/nodes/hourglass-split.svg";
    }
}
