using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes.Connections;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/ActionCommandNode/ActionCommandNode{.lang}.md")]
[Display(GroupName = "connections")]
public class ActionCommandNode : Node
{
    public override string TypeId => "core.ActionCommandNode";

    public string[] FrontContextId { get; set; } = [];

    public ActionCommandNode()
    {
        Inputs = [];
        Color = "#7dd0d4";
        Outputs = [
            new NodeOutput(){ Label = "" },
        ];
        Icon = "_content/Mars.Nodes.Workspace/nodes/usercase-64.png";
    }

}
