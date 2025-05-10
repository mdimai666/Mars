using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/JsonNode/JsonNode{.lang}.md")]
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
        Icon = "_content/Mars.Nodes.Workspace/nodes/scenario-48.png";
    }

    public enum JsonNodeAction
    {
        Auto,
        toJsonString,
        toObject
    }
}
