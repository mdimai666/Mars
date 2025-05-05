using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/NodeFormEditor/Docs/JsonNode/JsonNode{.lang}.md")]
public class JsonNode : Node
{
    public string Property { get; set; } = "Payload";
    public JsonNodeAction Action { get; set; }
    public bool FormatJsonString { get; set; }

    public JsonNode()
    {
        haveInput = true;
        Color = "#debd5c";
        Outputs = new List<NodeOutput> { new NodeOutput() };
        Icon = "_content/NodeWorkspace/nodes/scenario-48.png";
    }

    public enum JsonNodeAction
    {
        Auto,
        toJsonString,
        toObject
    }
}
