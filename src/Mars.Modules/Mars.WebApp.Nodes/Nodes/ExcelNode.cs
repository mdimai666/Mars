using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;
using Mars.Nodes.Core;

namespace Mars.WebApp.Nodes.Nodes;

[FunctionApiDocument("./_content/Mars.WebApp.Nodes.Front/docs/ExcelNode/ExcelNode{.lang}.md")]
[Display(GroupName = "office")]
public class ExcelNode : Node
{
    public string TemplateFile { get; set; } = "";

    public ExcelNode()
    {
        Color = "#21a366";
        Icon = "_content/Mars.Nodes.Workspace/nodes/excel-48.png";
        Inputs = [new()];
        Outputs = [new()];
    }
}
