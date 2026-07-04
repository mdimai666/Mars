using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes.Diagnostics;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/Common/FeatureUnderDevelopment{.lang}.md")]
[Display(GroupName = "diagnostic")]
public class CounterNode : Node
{
    public override string TypeId => "core.CounterNode";

    public int SegmentDisplayWidth = 95;
    public int SegmentDisplayHeight = 37;

    public override float BodyRectWidth => SegmentDisplayWidth + 20;
    public override float BodyRectHeight => base.BodyRectHeight + SegmentDisplayHeight - BodyWirePortsGap + 4;

    public CounterNode()
    {
        Color = "#A9BBCF";
        Outputs = [new() { Label = "output count" }];
        Inputs = [new() { Label = "increment" }, new() { Label = "substract" }, new() { Label = "reset" }];
        Icon = "_content/Mars.Nodes.Workspace/nodes/box-arrow-in-right.svg";
    }

}
