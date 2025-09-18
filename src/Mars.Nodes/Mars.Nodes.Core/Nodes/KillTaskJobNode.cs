using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/KillTaskJobNode/KillTaskJobNode{.lang}.md")]
[Display(GroupName = "task")]
public class KillTaskJobNode : Node
{
    public KillTaskJobNode()
    {
        Inputs = [new()];
        Outputs = [new() { Label = "New task job" }];
        Color = "#e16538";
        Icon = "_content/Mars.Nodes.Workspace/nodes/loop-start.svg";
    }
}
