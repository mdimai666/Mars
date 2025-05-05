using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/NodeFormEditor/Docs/TemplateNode/TemplateNode{.lang}.md")]
public class TemplateNode : Node
{
    public const string DefaultLanguage = "handlebars";

    public string Language { get; set; } = DefaultLanguage;
    public string Template { get; set; } = "<div> Template: {{Payload}} </div>";

    public TemplateNode()
    {
        haveInput = true;
        Color = "#ecb56a";
        Outputs = new List<NodeOutput> { new NodeOutput() };
        Icon = "_content/NodeWorkspace/nodes/scenario-48.png";
    }

}
