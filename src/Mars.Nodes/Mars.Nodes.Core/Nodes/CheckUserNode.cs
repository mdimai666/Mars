using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/CheckUserNode/CheckUserNode{.lang}.md")]
[Display(GroupName = "user")]
public class CheckUserNode : Node
{
    public CheckUserNode()
    {
        Inputs = [new()];
        Color = "#4cb5e6";
        Outputs = [
            new(){ Label = "Auth" },
            new(){ Label = "Non auth" },
        ];
        Icon = "_content/Mars.Nodes.Workspace/nodes/person-bounding-box.svg";
    }
}
