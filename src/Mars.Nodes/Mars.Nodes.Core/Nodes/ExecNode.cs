using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/NodeFormEditor/Docs/ExecNode/ExecNode{.lang}.md")]
public class ExecNode : Node
{
    public string Command { get; set; } = "pwsh.exe";

    public bool Append { get; set; } = true;

    public ExecNode()
    {
        haveInput = true;
        Outputs = new List<NodeOutput> { new NodeOutput() };
    }

}
