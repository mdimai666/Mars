using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/TemplateNode/TemplateNode{.lang}.md")]
[Display(GroupName = "function")]
public class TemplateNode : Node
{
    public const string DefaultLanguage = "handlebars";

    public string Language { get; set; } = DefaultLanguage;
    public string Template { get; set; } = "<div> Template: {{Payload}} </div>";

    public TemplateNode()
    {
        HaveInput = true;
        Color = "#ecb56a";
        Outputs = new List<NodeOutput> { new NodeOutput() };
        Icon = "_content/Mars.Nodes.Workspace/nodes/scenario-48.png";
    }

}
