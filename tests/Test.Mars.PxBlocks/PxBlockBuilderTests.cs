using System.Text.Json.Nodes;
using Mars.PxBlocks.Shared.Definitions;

namespace Test.Mars.PxBlocks;

public class PxBlockBuilderTests
{
    private sealed class DemoSet : PxBlockSet
    {
        public DemoSet()
        {
            Add(PxMaster.Define("set_a").Message("первый"));
            Add(PxMaster.Define("set_b").Output("Number").Message("число {NUM}", PxMaster.Number("NUM", 5)));
        }
    }

    [Fact]
    public void Fluent_StatementBlock()
    {
        PxBlockDefinition def = PxMaster.Define("mission_start")
            .Statement()
            .Colour("#107C10")
            .Tooltip("Старт миссии")
            .Message("старт со скоростью {speed} и делать {DO}",
                PxMaster.Number("speed", defl: 10, min: 0, max: 100),
                PxMaster.Do("DO"));

        var node = JsonNode.Parse(def.ToJson())!.AsObject();

        Assert.Equal("mission_start", (string)node["type"]!);
        Assert.Equal("старт со скоростью %1 и делать %2", (string)node["message0"]!);
        Assert.Equal("#107C10", (string)node["colour"]!);
        Assert.Equal("Старт миссии", (string)node["tooltip"]!);
        Assert.True(node.ContainsKey("previousStatement"));
        Assert.True(node.ContainsKey("nextStatement"));
        Assert.False(node.ContainsKey("output"));

        var args = node["args0"]!.AsArray();
        var number = args[0]!.AsObject();
        Assert.Equal("field_number", (string)number["type"]!);
        Assert.Equal("speed", (string)number["name"]!);
        Assert.Equal(10, (double)number["value"]!);
        Assert.Equal(0, (double)number["min"]!);
        Assert.Equal(100, (double)number["max"]!);

        var statement = args[1]!.AsObject();
        Assert.Equal("input_statement", (string)statement["type"]!);
        Assert.Equal("DO", (string)statement["name"]!);
    }

    [Fact]
    public void Fluent_OutputBlock()
    {
        PxBlockDefinition def = PxMaster.Define("demo_flag")
            .Output("Boolean")
            .Colour("#5C2D91")
            .Message("флаг {TEXT}", PxMaster.Text("TEXT", "вкл"));

        var node = JsonNode.Parse(def.ToJson())!.AsObject();

        Assert.Equal("Boolean", (string)node["output"]!);
        Assert.False(node.ContainsKey("previousStatement"));
        Assert.False(node.ContainsKey("nextStatement"));

        var arg = node["args0"]!.AsArray()[0]!.AsObject();
        Assert.Equal("field_input", (string)arg["type"]!);
        Assert.Equal("вкл", (string)arg["text"]!);
    }

    [Fact]
    public void Fluent_Dropdown()
    {
        PxBlockDefinition def = PxMaster.Define("demo_mode")
            .Statement()
            .Message("режим {MODE}", PxMaster.Dropdown("MODE", ("быстрый", "fast"), ("медленный", "slow")));

        var node = JsonNode.Parse(def.ToJson())!.AsObject();
        var options = node["args0"]!.AsArray()[0]!["options"]!.AsArray();

        Assert.Equal(2, options.Count);
        Assert.Equal("быстрый", (string)options[0]!.AsArray()[0]!);
        Assert.Equal("slow", (string)options[1]!.AsArray()[1]!);
    }

    [Fact]
    public void NamedPlaceholders_OrderFollowsMessage()
    {
        PxBlockDefinition def = PxMaster.Define("reordered")
            .Message("{A} и {B}", PxMaster.Text("B", "б"), PxMaster.Text("A", "а"));

        var node = JsonNode.Parse(def.ToJson())!.AsObject();

        Assert.Equal("%1 и %2", (string)node["message0"]!);
        var args = node["args0"]!.AsArray();
        Assert.Equal("A", (string)args[0]!["name"]!);
        Assert.Equal("а", (string)args[0]!["text"]!);
        Assert.Equal("B", (string)args[1]!["name"]!);
        Assert.Equal("б", (string)args[1]!["text"]!);
    }

    [Fact]
    public void NamedPlaceholders_UndeclaredHole_Throws()
    {
        PxBlockDefinition def = PxMaster.Define("bad_hole")
            .Message("значение {X}", PxMaster.Number("Y"));

        Assert.Throws<InvalidOperationException>(def.ToJson);
    }

    [Fact]
    public void NamedPlaceholders_UnusedArg_Throws()
    {
        PxBlockDefinition def = PxMaster.Define("bad_unused")
            .Message("значение {Y}", PxMaster.Number("X"), PxMaster.Number("Y"));

        Assert.Throws<InvalidOperationException>(def.ToJson);
    }

    [Fact]
    public void NamedPlaceholders_DuplicateHole_Throws()
    {
        PxBlockDefinition def = PxMaster.Define("bad_duplicate")
            .Message("{X} и ещё {X}", PxMaster.Number("X"));

        Assert.Throws<InvalidOperationException>(def.ToJson);
    }

    [Fact]
    public void PositionalPlaceholders_StillSupported()
    {
        var def = new PxBlockDefinition { TypeId = "legacy", Colour = "#000000" };
        def.Messages.Add(new PxMessageRow
        {
            Message = "принять %1",
            Args = [new PxValueInput { Name = "VAL", Check = ["Number"] }]
        });

        var node = JsonNode.Parse(def.ToJson())!.AsObject();

        Assert.Equal("принять %1", (string)node["message0"]!);
        Assert.Equal("VAL", (string)node["args0"]!.AsArray()[0]!["name"]!);
    }

    [Fact]
    public void Fluent_Hat_AddsPxHatExtension()
    {
        PxBlockDefinition def = PxMaster.Define("demo_event")
            .NoPrevious().NoNext().Hat()
            .Message("старт")
            .Message("%1", PxMaster.Do("DO"));

        var node = JsonNode.Parse(def.ToJson())!.AsObject();

        // style.hat не используем: jsonInit Blockly обнуляет style в общем определении,
        // и шапка доставалась бы только первому экземпляру блока.
        Assert.False(node.ContainsKey("style"));
        var extensions = node["extensions"]!.AsArray();
        Assert.Contains(extensions, e => (string?)e == "px_hat_cap");
    }

    [Fact]
    public void PxBlockSet_EnumeratesDeclaredBlocks()
    {
        var set = new DemoSet();

        Assert.Equal(2, set.Definitions.Count);
        Assert.Equal(["set_a", "set_b"], set.Select(d => d.TypeId).ToArray());
    }
}
