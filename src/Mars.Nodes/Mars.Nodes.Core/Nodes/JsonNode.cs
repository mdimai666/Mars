using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/JsonNode/JsonNode{.lang}.md")]
[Display(GroupName = "parser")]
public class JsonNode : Node
{
    public string Property { get; set; } = "Payload";
    public JsonNodeAction Action { get; set; }
    public bool FormatJsonString { get; set; }

    public JsonNode()
    {
        Inputs = [new()];
        Color = "#debd5c";
        Outputs = [new()];
        Icon = "_content/Mars.Nodes.Workspace/nodes/scenario-48.png";
    }

    public enum JsonNodeAction
    {
        Auto,
        ToJsonString,
        ToObject
    }
}
