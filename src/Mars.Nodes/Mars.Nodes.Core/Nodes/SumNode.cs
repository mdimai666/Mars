using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/SumNode/SumNode{.lang}.md")]
public class SumNode : Node
{
    [Required]
    public int a { get; set; } = 1;

    InputSource input1 = new();

    public int b { get; set; } = 1;

    public SumNode()
    {
        HaveInput = true;
        Outputs = new List<NodeOutput> { new NodeOutput() };
    }

}
