using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/FunctionNode/FunctionNode{.lang}.md")]
[Display(GroupName = "function")]
public class FunctionNode : Node
{
    public string Code { get; set; } = "return 1;";

    public FunctionNode()
    {
        Inputs = [new()];
        Color = "#F8D0A3";
        Outputs = new List<NodeOutput> { new NodeOutput() };
        Icon = "_content/Mars.Nodes.Workspace/nodes/csproj-48.png";
    }

}
