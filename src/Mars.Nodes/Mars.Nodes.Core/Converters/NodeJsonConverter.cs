using System.Text.Json;
using Mars.Nodes.Core.Nodes.Common;

namespace Mars.Nodes.Core.Converters;

public class NodeJsonConverter : System.Text.Json.Serialization.JsonConverter<Node>
{
    private readonly INodesLocator _nodesLocator;

    public NodeJsonConverter(INodesLocator nodesLocator)
    {
        _nodesLocator = nodesLocator;
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(Node) || typeToConvert == typeof(UnknownNode);
    }

    public override Node Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var jObj = doc.RootElement;

        if (!jObj.TryGetProperty(nameof(Node.TypeId), out var typeIdProp)
            && !jObj.TryGetProperty("typeId", out typeIdProp)
            && !jObj.TryGetProperty("Type", out typeIdProp))
        {
            //var ju = jObj.ToString();
            throw new JsonException("Property 'TypeId' is missing in Node JSON.");
        }

        var nodeTypeId = typeIdProp.GetString();
        if (string.IsNullOrWhiteSpace(nodeTypeId))
            throw new JsonException("Node.TypeId is null or empty.");

        var type = _nodesLocator.GetTypeByTypeId(nodeTypeId!);
        if (type is null)
        {
            var basic = jObj.Deserialize<NodeBasicObj>(options)!;
            return new UnknownNode(basic, jObj.ToString() ?? "");
        }
        var model = (Node)jObj.Deserialize(type, options)!;

        return model;
    }

    public override void Write(Utf8JsonWriter writer, Node node, JsonSerializerOptions options)
    {

        if (node is UnknownNode unknownNode)
        {
            using var doc = JsonDocument.Parse(unknownNode.JsonBody);
            doc.RootElement.WriteTo(writer);
            return;
        }
        var type = _nodesLocator.GetTypeByTypeId(node.TypeId)
                        ?? throw new JsonException($"Node.TypeId '{node.TypeId}' not found.");
        JsonSerializer.Serialize(writer, node, type, options);
    }
}
