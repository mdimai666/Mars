using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/ActionCommandNode/ActionCommandNode{.lang}.md")]
[Display(GroupName = "user")]
public class ActionCommandNode : Node
{
    public string[] FrontContextId { get; set; } = [];

    public ActionCommandNode()
    {
        Inputs = [];
        Color = "#7dd0d4";
        Outputs = new List<NodeOutput> {
            new NodeOutput(){ Label = "" },
        };
        Icon = "_content/Mars.Nodes.Workspace/nodes/loop.svg";
    }

}
