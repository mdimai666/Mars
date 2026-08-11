using System.Text.Json.Nodes;
using Mars.PxBlocks.Shared.Types;

namespace Test.Mars.PxBlocks;

public class PxTypeRegistryTests
{
    [Fact]
    public void CreateDefault_ContainsPxtShapes()
    {
        var registry = PxTypeRegistry.CreateDefault();

        var boolean = Assert.Single(registry.Types, t => t.Name == "Boolean");
        Assert.Equal(PxShape.Hexagonal, boolean.Shape);

        var any = Assert.Single(registry.Types, t => t.Name == "Any");
        Assert.Contains("*", any.CompatibleWith);

        var obj = Assert.Single(registry.Types, t => t.Name == "Object");
        Assert.Equal(PxShape.Square, obj.Shape);
    }

    [Fact]
    public void ToJson_SerializesMatrix()
    {
        var registry = new PxTypeRegistry
        {
            Types =
            [
                new PxType { Name = "Boolean", Shape = PxShape.Hexagonal },
                new PxType { Name = "Any", CompatibleWith = ["*"] }
            ]
        };

        var root = JsonNode.Parse(registry.ToJson())!.AsObject();
        var types = root["types"]!.AsArray();

        Assert.Equal(2, types.Count);

        var boolean = types[0]!.AsObject();
        Assert.Equal("Boolean", (string)boolean["name"]!);
        Assert.Equal("hexagonal", (string)boolean["shape"]!);

        var any = types[1]!.AsObject();
        Assert.Equal("rounded", (string)any["shape"]!);
        Assert.Equal("*", (string)any["compatibleWith"]!.AsArray()[0]!);
    }
}
