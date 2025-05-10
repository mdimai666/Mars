using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/HtmlParseNode/HtmlParseNode{.lang}.md")]
public class HtmlParseNode : Node
{
    [Required]
    public string Selector { get; set; } = "";
    public HtmlParseNodeOutput Output { get; set; }
    public HtmlParseInputMapping[] InputMappings { get; set; } = [new()];
    public bool ReturnEachObjectAsMessage { get; set; }
    public bool DontReturnMessageIfNothingFound { get; set; }

    public HtmlParseNode()
    {
        haveInput = true;
        Color = "#ecb56a";
        Outputs = new List<NodeOutput> { new NodeOutput() };
        Icon = "_content/Mars.Nodes.Workspace/nodes/scenario-48.png";
    }
}

public enum HtmlParseNodeOutput
{
    Html,
    Text,
    MapToObjects
}

public class HtmlParseInputMapping
{
    [Display(Name = "Sub elements selector")]
    public string Selector { get; set; } = "";

    [Display(Name = "Return value")]
    public InputMappingReturnValue ReturnValue { get; set; }

    [Display(Name = "Attribute name")]
    public string Attribute { get; set; } = "";

    [Display(Name = "Output field")]
    public string OutputField { get; set; } = "";

}

public enum InputMappingReturnValue
{
    Text,
    Html,
    Attribute
}
