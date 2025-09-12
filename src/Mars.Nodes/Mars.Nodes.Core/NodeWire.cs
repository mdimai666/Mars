using System.ComponentModel;
using System.Text.Json.Serialization;
using Mars.Nodes.Core.Converters;

namespace Mars.Nodes.Core;

[TypeConverter(typeof(NodeWireTypeConverter))]
[JsonConverter(typeof(NodeWireJsonConverter))]
public record NodeWire(string NodeId, int Input = 0)
{
    public override string ToString()
    {
        return Input > 0 ? $"{NodeId}#{Input}" : NodeId;
    }

    public static NodeWire Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentNullException(nameof(value));

        var parts = value.Split('#', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1)
            return new NodeWire(parts[0], 0);

        if (parts.Length == 2 && int.TryParse(parts[1], out var input))
            return new NodeWire(parts[0], input);

        throw new FormatException($"Invalid NodeWire format: {value}");
    }

    // implicit conversion to string
    public static implicit operator string(NodeWire wire) => wire.ToString();

    // implicit conversion from string
    public static implicit operator NodeWire(string value) => Parse(value);
}
