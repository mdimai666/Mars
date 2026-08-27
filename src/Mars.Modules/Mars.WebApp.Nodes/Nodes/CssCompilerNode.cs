using System.ComponentModel.DataAnnotations;
using Mars.Nodes.Core;
using Mars.Core.Attributes;

namespace Mars.WebApp.Nodes.Nodes;

[FunctionApiDocument("./_content/Mars.WebApp.Nodes.Front/docs/CssCompilerNode/CssCompilerNode{.lang}.md")]
[Display(GroupName = "compiler")]
public class CssCompilerNode : Node
{
    public CssCompilerNode()
    {
        Color = "#4f9ad5";
        Inputs = [new()];
        Outputs = [new() { Label = "compiled css" }];
        Icon = "_content/Mars.Nodes.Workspace/nodes/css.svg";
    }
}
