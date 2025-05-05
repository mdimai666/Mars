using Mars.Nodes.Core;

namespace Mars.Nodes.WebApp;

public class ExcelNode : Node
{
    public string TemplateFile { get; set; } = "";

    public ExcelNode()
    {
        haveInput = true;
        Color = "#21a366";
        Outputs = new List<NodeOutput> { new NodeOutput() };
        Icon = "_content/NodeWorkspace/nodes/excel-48.png";
    }
}
