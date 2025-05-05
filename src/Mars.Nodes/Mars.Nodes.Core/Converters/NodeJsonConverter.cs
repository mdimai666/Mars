using System.Text.Json;

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

        Type type = NodesLocator.GetTypeByFullName(nodeType!);
        dynamic model = jObj.Deserialize(type, options)!;

        return model;
    }

    public override void Write(Utf8JsonWriter writer, Node node, JsonSerializerOptions options)
    {
        JsonElement jsonElement = JsonSerializer.SerializeToElement(node, node.GetType(), options);
        jsonElement.WriteTo(writer);
        //writer.WriteStringValue(JsonSerializer.Serialize(node, options));
    }
}

public class NodeNewtonJsonConverter : Newtonsoft.Json.JsonConverter<Node>
{

    public override Node ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, Node? existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
    {

        Newtonsoft.Json.Linq.JObject jObject = Newtonsoft.Json.Linq.JObject.Load(reader);
        var json = jObject.ToString();
        string typeName = jObject.Value<string>(nameof(Node.Type)) ?? jObject.Value<string>("type") ?? throw new InvalidOperationException("Node.Type cannot read");
        Type type = NodesLocator.GetTypeByFullName(typeName);

        dynamic model = JsonSerializer.Deserialize(json, type)!;

        return model;

    }

    public override void WriteJson(Newtonsoft.Json.JsonWriter writer, Node? value, Newtonsoft.Json.JsonSerializer serializer)
    {

        string json = JsonSerializer.Serialize(value);
        writer.WriteRawValue(json);
    }
}
