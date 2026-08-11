using System.Text.Json.Nodes;
using Mars.PxBlocks.Shared.Toolbox;

namespace Test.Mars.PxBlocks;

public class PxToolboxTests
{
    [Fact]
    public void ToJson_CategoryToolbox_Structure()
    {
        var toolbox = new PxToolbox
        {
            Contents =
            [
                new PxToolboxCategory
                {
                    Name = "Логика",
                    Colour = "#006C9E",
                    Blocks = [new PxToolboxBlock { Type = "controls_if" }]
                },
                new PxToolboxSeparator { Colour = "#808080" },
                new PxToolboxCategory { Name = "Переменные", Colour = "#A80000", Custom = "VARIABLE" }
            ]
        };

        var root = JsonNode.Parse(toolbox.ToJson())!.AsObject();

        Assert.Equal("categoryToolbox", (string)root["kind"]!);

        var contents = root["contents"]!.AsArray();
        Assert.Equal(3, contents.Count);

        var logic = contents[0]!.AsObject();
        Assert.Equal("category", (string)logic["kind"]!);
        Assert.Equal("Логика", (string)logic["name"]!);
        Assert.Equal("#006C9E", (string)logic["colour"]!);

        var blocks = logic["contents"]!.AsArray();
        Assert.Equal("block", (string)blocks[0]!["kind"]!);
        Assert.Equal("controls_if", (string)blocks[0]!["type"]!);

        var sep = contents[1]!.AsObject();
        Assert.Equal("sep", (string)sep["kind"]!);
        Assert.Equal("#808080", (string)sep["colour"]!);

        var vars = contents[2]!.AsObject();
        Assert.Equal("VARIABLE", (string)vars["custom"]!);
    }

    [Fact]
    public void ToJson_FlyoutToolbox_WhenNoCategories()
    {
        var toolbox = new PxToolbox
        {
            Contents =
            [
                new PxToolboxBlock { Type = "math_number", FieldsJson = """{"NUM": 42}""" }
            ]
        };

        var root = JsonNode.Parse(toolbox.ToJson())!.AsObject();

        Assert.Equal("flyoutToolbox", (string)root["kind"]!);

        var block = root["contents"]!.AsArray()[0]!.AsObject();
        Assert.Equal("math_number", (string)block["type"]!);
        Assert.Equal(42, (int)block["fields"]!["NUM"]!);
    }
}
