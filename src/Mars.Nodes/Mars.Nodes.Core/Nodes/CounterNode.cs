using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/Common/FeatureUnderDevelopment{.lang}.md")]
[Display(GroupName = "diagnostic")]
public class CounterNode : Node
{
    public CounterNode()
    {
        Color = "#A9BBCF";
        Outputs = [new() { Label = "output count" }];
        Inputs = [new() { Label = "substract" }, new() { Label = "increment" }];
        Icon = "_content/Mars.Nodes.Workspace/nodes/box-arrow-in-right.svg";
    }
}
