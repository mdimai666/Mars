using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;
using Mars.Nodes.Core;

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
