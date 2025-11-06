using System.Text.Encodings.Web;
using System.Text.Json;
using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Converters;

public class NodeJsonConverter : System.Text.Json.Serialization.JsonConverter<Node>
{
    private readonly NodesLocator _nodesLocator;

    public NodeJsonConverter(NodesLocator nodesLocator)
    {
        _nodesLocator = nodesLocator;
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(Node) || typeToConvert == typeof(UnknownNode);// || typeof(Node).IsAssignableFrom(typeToConvert);
    }

    public override Node Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var jObj = doc.RootElement;

        if (!jObj.TryGetProperty(nameof(Node.Type), out var typeProp) &&
            !jObj.TryGetProperty("type", out typeProp))
        {
            throw new JsonException("Property 'Type' is missing in Node JSON.");
        }

        var nodeType = typeProp.GetString();
        if (string.IsNullOrWhiteSpace(nodeType))
            throw new JsonException("Node.Type is null or empty.");

        var type = _nodesLocator.GetTypeByFullName(nodeType!);
        if (type is null)
        {
            var basic = jObj.Deserialize<NodeBasicImplement>(options)!;
            return new UnknownNode(basic, jObj.ToString() ?? "");
        }
        var model = (Node)jObj.Deserialize(type, options)!;

        return model;
    }

    //static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    //{
    //    WriteIndented = true,
    //    PropertyNameCaseInsensitive = true,
    //    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    //};

    public override void Write(Utf8JsonWriter writer, Node node, JsonSerializerOptions options)
    {

        if (node is UnknownNode unknownNode)
        {
            using var doc = JsonDocument.Parse(unknownNode.JsonBody);
            doc.RootElement.WriteTo(writer);
            return;
        }
        var fullName = node.GetType().FullName!;
        var type = _nodesLocator.GetTypeByFullName(fullName)
                        ?? throw new JsonException($"Node.Type '{fullName}' not found.");
        JsonSerializer.Serialize(writer, node, type, options);
    }
}
