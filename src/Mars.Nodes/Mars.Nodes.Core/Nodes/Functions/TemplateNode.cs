using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes.Functions;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/TemplateNode/TemplateNode{.lang}.md")]
[Display(GroupName = "functions")]
public class TemplateNode : Node
{
    public override string TypeId => "core.TemplateNode";

    public const string DefaultSyntaxLanguage = "handlebars";
    public const string DefaultTemplateEngineId = "Core.Handlebars";

    public string TemplateEngineId { get; set; } = DefaultTemplateEngineId;
    public string SyntaxLanguage { get; set; } = DefaultSyntaxLanguage;

    public string Template { get; set; } = "<div> Template: {{Payload}} </div>";
    public string Property { get; set; } = "Payload";

    public TemplateNode()
    {
        Inputs = [new()];
        Color = "#ecb56a";
        Outputs = [new()];
        Icon = "_content/Mars.Nodes.Workspace/nodes/scenario-48.png";
    }

}
