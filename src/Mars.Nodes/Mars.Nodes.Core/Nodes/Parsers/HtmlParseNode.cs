using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes.Parsers;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/HtmlParseNode/HtmlParseNode{.lang}.md")]
[Display(GroupName = "parser")]
public class HtmlParseNode : Node, INodeOutputValueSpec
{
    public override string TypeId => "core.HtmlParseNode";

    [Required]
    public string Selector { get; set; } = "";
    public HtmlParseNodeOutput Output { get; set; }
    public HtmlParseInputMapping[] InputMappings { get; set; } = [new()];
    public bool ReturnEachObjectAsMessage { get; set; }
    public bool DontReturnMessageIfNothingFound { get; set; }

    public HtmlParseNode()
    {
        Inputs = [new()];
        Color = "#ecb56a";
        Outputs = [new()];
        Icon = "_content/Mars.Nodes.Workspace/nodes/scenario-48.png";
    }

    public IEnumerable<OutputValueSpec> GetOutputValueSpec()
    {
        if (Output == HtmlParseNodeOutput.MapToObjects)
        {
            yield return new OutputValueSpec(nameof(NodeMsg.Payload),
                ReturnEachObjectAsMessage ? VarNode.ObjectTypeName : "object[]",
                Description: "mapped objects (field → string)");

            var root = ReturnEachObjectAsMessage ? nameof(NodeMsg.Payload) : "Payload[]";

            for (var i = 0; i < InputMappings.Length; i++)
            {
                var name = string.IsNullOrEmpty(InputMappings[i].OutputField) ? $"field{i + 1}" : InputMappings[i].OutputField;
                yield return new OutputValueSpec($"{root}.{name}", "string");
            }

            yield break;
        }

        var description = Output == HtmlParseNodeOutput.Html ? "element InnerHtml" : "element TextContent";

        if (ReturnEachObjectAsMessage)
        {
            yield return new OutputValueSpec(nameof(NodeMsg.Payload), "string", Description: description);
            yield break;
        }

        yield return new OutputValueSpec(nameof(NodeMsg.Payload), "string[]", Description: description);
        yield return new OutputValueSpec("Payload[]", "string");
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
