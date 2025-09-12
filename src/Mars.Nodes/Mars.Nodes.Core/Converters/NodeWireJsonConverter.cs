using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mars.Nodes.Core.Converters;

public class NodeWireJsonConverter : JsonConverter<NodeWire>
{
    public override NodeWire Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        return NodeWire.Parse(str!);
    }

    public override void Write(Utf8JsonWriter writer, NodeWire value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
