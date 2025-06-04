using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/ExecNode/ExecNode{.lang}.md")]
[Display(GroupName = "function")]
public class ExecNode : Node
{
    public string Command { get; set; } = "pwsh.exe";

    public bool Append { get; set; } = true;

    public ExecNode()
    {
        HaveInput = true;
        Outputs = new List<NodeOutput> { new NodeOutput() };
        Icon = "_content/Mars.Nodes.Workspace/nodes/terminal-fill.svg";
    }

}
