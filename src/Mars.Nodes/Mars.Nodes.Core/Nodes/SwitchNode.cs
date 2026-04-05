using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/SwitchNode/SwitchNode{.lang}.md")]
[Display(GroupName = "function")]
public class SwitchNode : Node
{
    public const string ElseConditionValue = "$else";

    public Condition[] Conditions { get => field; set { field = value; OutputCount = value.Length; } } = [
        new Condition() { Value = "Payload == 123" },
        new Condition() { Value = "Payload.ToString() == \"ok\""},
    ];

    public bool BreakAfterFirst { get; set; } = true;
    public bool UseElseOutput { get; set; }

    public SwitchNode()
    {
        Inputs = [new()];
        Color = "#E0D870";
        Outputs = [new(), new(),];
        Icon = "_content/Mars.Nodes.Workspace/nodes/option.svg";
    }

    public class Condition
    {
        public string Value { get; set; } = "";
    }
}
