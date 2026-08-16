using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes.Connections;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/ExecXActionNode/ExecXActionNode{.lang}.md")]
[Display(GroupName = "connections")]
public class ExecXActionNode : Node
{
    public override string TypeId => "core.ExecXActionNode";

    public string CommandId { get; set; } = "";

    /// <summary>
    /// Именованные аргументы вызова команды (имя=значение).
    /// </summary>
    public Dictionary<string, string> Args { get; set; } = [];

    public ExecXActionNode()
    {
        Inputs = [new()];
        Outputs = [new()];
        Icon = "_content/Mars.Nodes.Workspace/nodes/function.svg";
    }

}
