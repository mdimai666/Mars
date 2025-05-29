using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/SwitchNode/SwitchNode{.lang}.md")]
public class SwitchNode : Node
{
    public List<Condition> Conditions { get; set; } = [
        new Condition() { Key = "if1", Value = "Payload == 123" },
        new Condition() { Key = "if2", Value = "msg.Payload != 123" },
    ];

    public bool BreakAfterFirst { get; set; }

    public SwitchNode()
    {
        HaveInput = true;
        Color = "#E0D870";
        Outputs = new List<NodeOutput>
        {
            new NodeOutput(),
            new NodeOutput(),
        };
        Icon = "_content/Mars.Nodes.Workspace/nodes/option.svg";
    }

    public class Condition
    {
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
    }
}
