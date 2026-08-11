using System.Text.Json.Nodes;
using Mars.PxBlocks.Shared.Definitions;

namespace Test.Mars.PxBlocks;

public class PxBlockDefinitionTests
{
    private sealed class ValueDemoBlock : PxBlockDefinition
    {
        public ValueDemoBlock()
        {
            TypeId = "demo_value";
            Colour = "#712672";
            OutputType = "Number";
            Messages =
            [
                new PxMessageRow
                {
                    Message = "число %1",
                    Args = [new PxFieldNumber { Name = "NUM", Value = 7 }]
                }
            ];
        }
    }

    private sealed class StatementDemoBlock : PxBlockDefinition
    {
        public StatementDemoBlock()
        {
            TypeId = "demo_statement";
            Messages =
            [
                new PxMessageRow
                {
                    Message = "принять %1 и делать %2",
                    Args =
                    [
                        new PxValueInput { Name = "VAL", Check = ["Number"] },
                        new PxStatementInput { Name = "DO" }
                    ]
                }
            ];
            Extensions = ["demo_ext"];
            Mutator = "demo_mutator";
        }
    }

    [Fact]
    public void ToJson_ValueBlock()
    {
        var node = JsonNode.Parse(new ValueDemoBlock().ToJson())!.AsObject();

        Assert.Equal("demo_value", (string)node["type"]!);
        Assert.Equal("число %1", (string)node["message0"]!);
        Assert.Equal("Number", (string)node["output"]!);
        Assert.Equal("#712672", (string)node["colour"]!);
        Assert.False(node.ContainsKey("previousStatement"));

        var arg = node["args0"]!.AsArray()[0]!.AsObject();
        Assert.Equal("field_number", (string)arg["type"]!);
        Assert.Equal(7, (double)arg["value"]!);
    }

    [Fact]
    public void ToJson_StatementBlock()
    {
        var node = JsonNode.Parse(new StatementDemoBlock().ToJson())!.AsObject();

        Assert.False(node.ContainsKey("output"));
        Assert.True(node.ContainsKey("previousStatement"));
        Assert.True(node.ContainsKey("nextStatement"));
        Assert.Null(node["previousStatement"]);

        var args = node["args0"]!.AsArray();
        Assert.Equal("input_value", (string)args[0]!["type"]!);
        Assert.Equal("Number", (string)args[0]!["check"]!.AsArray()[0]!);
        Assert.Equal("input_statement", (string)args[1]!["type"]!);
        Assert.False(args[1]!.AsObject().ContainsKey("check"));

        Assert.Equal("demo_ext", (string)node["extensions"]!.AsArray()[0]!);
        Assert.Equal("demo_mutator", (string)node["mutator"]!);
    }

    [Fact]
    public void ToArrayJson_CombinesDefinitions()
    {
        var json = PxBlockDefinition.ToArrayJson([new ValueDemoBlock(), new StatementDemoBlock()]);

        var array = JsonNode.Parse(json)!.AsArray();
        Assert.Equal(2, array.Count);
        Assert.Equal("demo_value", (string)array[0]!["type"]!);
        Assert.Equal("demo_statement", (string)array[1]!["type"]!);
    }
}
