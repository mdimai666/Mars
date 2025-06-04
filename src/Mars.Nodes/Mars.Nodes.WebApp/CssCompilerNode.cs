using System.ComponentModel.DataAnnotations;
using Mars.Nodes.Core;

namespace Mars.Nodes.WebApp;

[Display(GroupName = "compiler")]
public class CssCompilerNode : Node
{
    public CssCompilerNode()
    {
        Color = "#4f9ad5";
        HaveInput = true;
        Outputs = new List<NodeOutput> { new NodeOutput() { Label = "compiled css" } };
        Icon = "_content/Mars.Nodes.Workspace/nodes/css.svg";
    }
}
