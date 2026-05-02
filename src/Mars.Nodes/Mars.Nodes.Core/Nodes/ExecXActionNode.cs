using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/ExecXActionNode/ExecXActionNode{.lang}.md")]
[Display(GroupName = "function")]
public class ExecXActionNode : Node
{
    public string CommandId { get; set; } = "";

    public ExecXActionNode()
    {
        Inputs = [new()];
        Outputs = [new()];
        Icon = "_content/Mars.Nodes.Workspace/nodes/function.svg";
    }

}
