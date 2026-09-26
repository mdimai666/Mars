using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes.Functions;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/docs/EvalNode/EvalNode{.lang}.md")]
[Display(GroupName = "functions")]
public class EvalNode : Node
{
    public override string TypeId => "core.EvalNode";

    public string ValueKind { get; set; } = InputValueKind.Expression;
    public string Input { get; set; } = "Payload + 1";

    public EvalNode()
    {
        Inputs = [new()];
        Color = "#d4eba1";
        Outputs = [new NodeOutput()];
        Icon = "_content/Mars.Nodes.Workspace/nodes/eval.svg";
    }

}
