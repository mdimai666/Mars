using System.Text.Json;
using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Converters;

public class NodeJsonConverter : System.Text.Json.Serialization.JsonConverter<Node>
{
    public override Node Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        JsonElement jObj = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
        string? nodeType;
        if (jObj.TryGetProperty(nameof(Node.Type), out var prop1)) nodeType = prop1.GetString();
        else if (jObj.TryGetProperty("type", out var prop2)) nodeType = prop2.GetString();
        else throw new InvalidOperationException("Node.Type cannot read");


        var type = NodesLocator.GetTypeByFullName(nodeType!);
        if (type is null)
        {
            var basic = jObj.Deserialize<NodeBasicImplement>(options)!;
            return new UnknownNode(basic, jObj.ToString() ?? "");
        }
        var model = (Node)jObj.Deserialize(type, options)!;

        return model;
    }

    public override void Write(Utf8JsonWriter writer, Node node, JsonSerializerOptions options)
    {
        if (node is UnknownNode unknownNode)
        {
            var unel = JsonSerializer.Deserialize<JsonElement>(unknownNode.JsonBody, options);
            unel.WriteTo(writer);
            return;
        }

        JsonElement jsonElement = JsonSerializer.SerializeToElement(node, node.GetType(), options);
        jsonElement.WriteTo(writer);
        //writer.WriteStringValue(JsonSerializer.Serialize(node, options));
    }
}
