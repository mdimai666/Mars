using System.ComponentModel.DataAnnotations;
using Mars.Nodes.Core;

namespace Mars.Nodes.WebApp.Nodes;

[Display(GroupName = "excel")]
public class ExcelNode : Node
{
    public string TemplateFile { get; set; } = "";

    public ExcelNode()
    {
        Inputs = [new()];
        Color = "#21a366";
        Outputs = new List<NodeOutput> { new NodeOutput() };
        Icon = "_content/Mars.Nodes.Workspace/nodes/excel-48.png";
    }
}
