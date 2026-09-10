using System.Text.Json.Nodes;
using Mars.PxBlocks.Runtime.Values;

namespace Test.Mars.PxBlocks;

/// <summary>JSON → PxValue: начальные переменные запуска и обмен значениями с хостом.</summary>
public class PxValueJsonTests
{
    [Fact]
    public void FromJson_NullNode_ReturnsNullValue()
        => Assert.Same(PxNullValue.Instance, PxValueJson.FromJson(null));

    [Fact]
    public void FromJson_JsonNullLiteral_ReturnsNullValue()
        => Assert.Same(PxNullValue.Instance, PxValueJson.FromJson(JsonNode.Parse("null")));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FromJson_Boolean(bool value)
        => Assert.Equal(new PxBooleanValue(value), PxValueJson.FromJson(JsonValue.Create(value)));

    [Theory]
    [InlineData(0)]
    [InlineData(42)]
    [InlineData(-3.5)]
    public void FromJson_Number(double value)
        => Assert.Equal(new PxNumberValue(value), PxValueJson.FromJson(JsonValue.Create(value)));

    [Fact]
    public void FromJson_String()
    {
        Assert.Equal(new PxStringValue("Байкал"), PxValueJson.FromJson(JsonValue.Create("Байкал")));
        Assert.Equal(PxStringValue.Empty, PxValueJson.FromJson(JsonValue.Create("")));
    }

    [Fact]
    public void FromJson_Object_ConvertsMembersRecursively()
    {
        var node = JsonNode.Parse("""{ "name": "Байкал", "depth": 1642, "deep": true, "tags": ["lake", "ice"] }""");

        var value = Assert.IsType<PxObjectValue>(PxValueJson.FromJson(node));

        Assert.Equal(new PxStringValue("Байкал"), value.Members["name"]);
        Assert.Equal(new PxNumberValue(1642), value.Members["depth"]);
        Assert.Equal(PxBooleanValue.True, value.Members["deep"]);
        var tags = Assert.IsType<PxListValue>(value.Members["tags"]);
        Assert.Equal([new PxStringValue("lake"), new PxStringValue("ice")], tags.Items);
    }

    [Fact]
    public void FromJson_Array_ConvertsItemsRecursively()
    {
        var node = JsonNode.Parse("""[1, "two", false, null, [3]]""");

        var value = Assert.IsType<PxListValue>(PxValueJson.FromJson(node));

        Assert.Equal(5, value.Items.Count);
        Assert.Equal(new PxNumberValue(1), value.Items[0]);
        Assert.Equal(new PxStringValue("two"), value.Items[1]);
        Assert.Equal(PxBooleanValue.False, value.Items[2]);
        Assert.Same(PxNullValue.Instance, value.Items[3]);
        Assert.Equal([new PxNumberValue(3)], Assert.IsType<PxListValue>(value.Items[4]).Items);
    }

    [Fact]
    public void FromJson_NumberCreatedInMemory_ConvertsRegardlessOfBackingType()
    {
        // Хост собирает запрос в памяти: int/long/decimal/float — не JsonElement,
        // TryGetValue<double> на них не срабатывает.
        Assert.Equal(new PxNumberValue(42), PxValueJson.FromJson(JsonValue.Create(42)));
        Assert.Equal(new PxNumberValue(42), PxValueJson.FromJson(JsonValue.Create(42L)));
        Assert.Equal(new PxNumberValue(1.5), PxValueJson.FromJson(JsonValue.Create(1.5m)));
        Assert.Equal(new PxNumberValue(2), PxValueJson.FromJson(JsonValue.Create(2.0f)));

        var node = new JsonObject { ["x"] = JsonValue.Create(5) };
        Assert.Equal(new PxNumberValue(5), Assert.IsType<PxObjectValue>(PxValueJson.FromJson(node)).Members["x"]);
    }

    [Fact]
    public void FromJson_ValueWithoutPrimitiveMapping_Throws()
    {
        // Такого значения штатный JSON не порождает — ветка защитная.
        var node = JsonValue.Create(new object());

        Assert.Throws<InvalidOperationException>(() => PxValueJson.FromJson(node));
    }
}
