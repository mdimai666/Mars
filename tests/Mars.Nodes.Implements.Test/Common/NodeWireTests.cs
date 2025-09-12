using System.ComponentModel;
using System.Text.Json;
using Mars.Nodes.Core;

namespace Mars.Nodes.Implements.Test.Common;

public class NodeWireTests
{
    [Fact]
    public void Parse_WithGuidAndInput_ReturnsCorrectNodeWire()
    {
        // Arrange
        var guid = Guid.NewGuid().ToString();
        var input = 3;
        var str = $"{guid}#{input}";

        // Act
        var wire = NodeWire.Parse(str);

        // Assert
        Assert.Equal(guid, wire.NodeId);
        Assert.Equal(input, wire.Input);
    }

    [Fact]
    public void Parse_WithOnlyGuid_ReturnsNodeWireWithZeroInput()
    {
        // Arrange
        var guid = Guid.NewGuid().ToString();

        // Act
        var wire = NodeWire.Parse(guid);

        // Assert
        Assert.Equal(guid, wire.NodeId);
        Assert.Equal(0, wire.Input);
    }

    [Fact]
    public void ToString_WithInputGreaterThanZero_ReturnsGuidAndInput()
    {
        // Arrange
        var wire = new NodeWire("abc", 2);

        // Act
        var result = wire.ToString();

        // Assert
        Assert.Equal("abc#2", result);
    }

    [Fact]
    public void ToString_WithZeroInput_ReturnsOnlyGuid()
    {
        // Arrange
        var wire = new NodeWire("abc", 0);

        // Act
        var result = wire.ToString();

        // Assert
        Assert.Equal("abc", result);
    }

    [Fact]
    public void ImplicitConversion_FromString_WorksCorrectly()
    {
        // Arrange
        var str = "abc#7";

        // Act
        NodeWire wire = str;

        // Assert
        Assert.Equal("abc", wire.NodeId);
        Assert.Equal(7, wire.Input);
    }

    [Fact]
    public void ImplicitConversion_ToString_WorksCorrectly()
    {
        // Arrange
        var wire = new NodeWire("xyz", 4);

        // Act
        string str = wire;

        // Assert
        Assert.Equal("xyz#4", str);
    }

    [Fact]
    public void JsonSerialization_AndDeserialization_WorksCorrectly()
    {
        // Arrange
        var wire = new NodeWire("abc", 5);

        // Act
        var json = JsonSerializer.Serialize(wire);
        var deserialized = JsonSerializer.Deserialize<NodeWire>(json);

        // Assert
        Assert.Equal("\"abc#5\"", json);
        Assert.Equal(wire, deserialized);
    }

    [Fact]
    public void TypeConverter_CanConvertFromString()
    {
        // Arrange
        var converter = TypeDescriptor.GetConverter(typeof(NodeWire));

        // Act
        var result = converter.ConvertFrom("test#9") as NodeWire;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", result!.NodeId);
        Assert.Equal(9, result.Input);
    }

    [Fact]
    public void TypeConverter_CanConvertToString()
    {
        // Arrange
        var converter = TypeDescriptor.GetConverter(typeof(NodeWire));
        var wire = new NodeWire("zzz", 1);

        // Act
        var result = converter.ConvertTo(wire, typeof(string));

        // Assert
        Assert.Equal("zzz#1", result);
    }
}
