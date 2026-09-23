using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes.Parsers;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/JsonNode/JsonNode{.lang}.md")]
[Display(GroupName = "parser")]
public class JsonNode : Node, INodeOutputValueSpec
{
    public override string TypeId => "core.JsonNode";

    public string Property { get; set; } = "Payload";
    public JsonNodeAction Action { get; set; }
    public bool FormatJsonString { get; set; }

    public JsonNode()
    {
        Inputs = [new()];
        Color = "#debd5c";
        Outputs = [new()];
        Icon = "_content/Mars.Nodes.Workspace/nodes/json.svg";
    }

    public IEnumerable<OutputValueSpec> GetOutputValueSpec()
    {
        var target = string.IsNullOrWhiteSpace(Property) ? nameof(NodeMsg.Payload) : Property;

        yield return Action == JsonNodeAction.ToJsonString
            ? new OutputValueSpec(target, "string")
            : new OutputValueSpec(target, VarNode.ObjectTypeName,
                Description: Action == JsonNodeAction.ToObject
                    ? "DynamicJson"
                    : "DynamicJson for string input, JSON string otherwise");
    }

    public enum JsonNodeAction
    {
        Auto,
        ToJsonString,
        ToObject
    }
}
