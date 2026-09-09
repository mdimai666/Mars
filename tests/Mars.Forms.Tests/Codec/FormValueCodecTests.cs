using System.Globalization;
using System.Text.Json.Nodes;
using FluentAssertions;
using Mars.Forms.Contracts;

namespace Mars.Forms.Tests.Codec;

public class FormValueCodecTests
{
    [Theory]
    [InlineData("abc", FormFieldType.String, "abc")]
    [InlineData("abc", FormFieldType.Text, "abc")]
    [InlineData("in_progress", FormFieldType.Select, "in_progress")]
    public void FromClr_String_LikeTypes_RoundTrips(object value, FormFieldType type, string expected)
    {
        var node = FormValueCodec.FromClr(value, type);

        node!.GetValue<string>().Should().Be(expected);
        FormValueCodec.TryToClr(node, type, out var read, out var error).Should().BeTrue();
        error.Should().BeNull();
        read.Should().Be(expected);
    }

    [Fact]
    public void FromClr_Bool_RoundTrips()
    {
        var node = FormValueCodec.FromClr(true, FormFieldType.Bool);

        node!.GetValue<bool>().Should().BeTrue();
        FormValueCodec.TryToClr(node, FormFieldType.Bool, out var read, out _).Should().BeTrue();
        read.Should().Be(true);
    }

    [Fact]
    public void TryToClr_Bool_AcceptsStringAndFlag()
    {
        FormValueCodec.TryToClr(JsonValue.Create("true"), FormFieldType.Bool, out var fromText, out _).Should().BeTrue();
        fromText.Should().Be(true);

        FormValueCodec.TryToClr(JsonValue.Create(1), FormFieldType.Bool, out var fromNumber, out _).Should().BeTrue();
        fromNumber.Should().Be(true);
    }

    [Fact]
    public void TryToClr_Int_BoxesAsInt32_AndRejectsOverflow()
    {
        FormValueCodec.TryToClr(JsonValue.Create(42), FormFieldType.Int, out var value, out var error).Should().BeTrue();
        value.Should().BeOfType<int>().Which.Should().Be(42);
        error.Should().BeNull();

        FormValueCodec.TryToClr(JsonValue.Create(5_000_000_000L), FormFieldType.Int, out _, out var overflow)
                        .Should().BeFalse();
        overflow.Should().Be("число вне диапазона Int32");
    }

    [Fact]
    public void TryToClr_Long_BoxesAsInt64()
    {
        FormValueCodec.TryToClr(JsonValue.Create(5_000_000_000L), FormFieldType.Long, out var value, out _).Should().BeTrue();
        value.Should().BeOfType<long>().Which.Should().Be(5_000_000_000L);
    }

    [Fact]
    public void TryToClr_IntegerTypes_RejectFraction()
    {
        FormValueCodec.TryToClr(JsonValue.Create(1.5), FormFieldType.Int, out _, out var error).Should().BeFalse();
        error.Should().Be("ожидается целое число");
    }

    [Fact]
    public void Decimal_GoesOnWireAsString_ToKeepPrecision()
    {
        var node = FormValueCodec.FromClr(10.5m, FormFieldType.Decimal);

        node!.GetValue<string>().Should().Be("10.5");
        FormValueCodec.TryToClr(node, FormFieldType.Decimal, out var read, out _).Should().BeTrue();
        read.Should().Be(10.5m);
    }

    [Fact]
    public void TryToClr_Decimal_AcceptsJsonNumber()
    {
        FormValueCodec.TryToClr(JsonValue.Create(10.5), FormFieldType.Decimal, out var read, out _).Should().BeTrue();
        read.Should().Be(10.5m);
    }

    [Fact]
    public void TryToClr_Decimal_RejectsGarbageText()
    {
        FormValueCodec.TryToClr(JsonValue.Create("abc"), FormFieldType.Decimal, out _, out var error).Should().BeFalse();
        error.Should().Contain("decimal");
    }

    [Fact]
    public void DateTime_GoesOnWireAsIso8601_AndRoundTrips()
    {
        var value = new DateTimeOffset(2026, 9, 9, 12, 30, 0, TimeSpan.FromHours(9));

        var node = FormValueCodec.FromClr(value, FormFieldType.DateTime);
        var text = node!.GetValue<string>();

        text.Should().Be(value.ToString("O", CultureInfo.InvariantCulture));
        FormValueCodec.TryToClr(node, FormFieldType.DateTime, out var read, out _).Should().BeTrue();
        read.Should().Be(value);
    }

    [Fact]
    public void TryToClr_DateTime_RejectsGarbage()
    {
        FormValueCodec.TryToClr(JsonValue.Create("вчера"), FormFieldType.DateTime, out _, out var error).Should().BeFalse();
        error.Should().Contain("ISO-8601");
    }

    [Fact]
    public void Relation_GoesOnWireAsGuidString()
    {
        var id = Guid.NewGuid();

        var node = FormValueCodec.FromClr(id, FormFieldType.Relation);

        node!.GetValue<string>().Should().Be(id.ToString("D"));
        FormValueCodec.TryToClr(node, FormFieldType.Relation, out var read, out _).Should().BeTrue();
        read.Should().Be(id);
    }

    [Fact]
    public void TryToClr_Relation_RejectsNonGuid()
    {
        FormValueCodec.TryToClr(JsonValue.Create("not-a-guid"), FormFieldType.File, out _, out var error).Should().BeFalse();
        error.Should().Contain("Guid");
    }

    [Fact]
    public void SelectMany_RoundTripsAsArrayOfKeys()
    {
        var node = FormValueCodec.FromClr(new[] { "a", "b" }, FormFieldType.SelectMany);

        node.Should().BeOfType<JsonArray>().Which.Count.Should().Be(2);
        FormValueCodec.TryToClr(node, FormFieldType.SelectMany, out var read, out _).Should().BeTrue();
        read.Should().BeEquivalentTo(new[] { "a", "b" });
    }

    [Fact]
    public void Object_PassesNodeThrough()
    {
        var source = new JsonObject { ["nested"] = new JsonArray(1, 2) };

        var node = FormValueCodec.FromClr(source, FormFieldType.Object);

        node.Should().NotBeSameAs(source);
        FormValueCodec.TryToClr(node, FormFieldType.Object, out var read, out _).Should().BeTrue();
        read.Should().BeOfType<JsonObject>();
    }

    [Fact]
    public void Computed_NeverCarriesValue()
    {
        FormValueCodec.FromClr("что угодно", FormFieldType.Computed).Should().BeNull();
        FormValueCodec.TryToClr(JsonValue.Create(1), FormFieldType.Computed, out _, out var error).Should().BeFalse();
        error.Should().Contain("вычислимое");
    }

    [Fact]
    public void FromClr_Null_ReturnsNull()
    {
        FormValueCodec.FromClr(null, FormFieldType.String).Should().BeNull();
    }

    [Fact]
    public void TryToClr_NullNode_IsNotAnError()
    {
        FormValueCodec.TryToClr(null, FormFieldType.String, out var value, out var error).Should().BeTrue();
        value.Should().BeNull();
        error.Should().BeNull();
    }

    [Fact]
    public void TryToClrList_KeepsOrderAsIndex_AndReportsElementPosition()
    {
        var array = new JsonArray(JsonValue.Create("a"), JsonValue.Create(5));

        FormValueCodec.TryToClrList(array, FormFieldType.String, out var values, out var error).Should().BeTrue();
        values.Should().Equal("a", "5");
        error.Should().BeNull();

        var broken = new JsonArray(new JsonObject());
        FormValueCodec.TryToClrList(broken, FormFieldType.String, out _, out var elementError).Should().BeFalse();
        elementError.Should().StartWith("элемент 0:");
    }

    [Fact]
    public void TryToClrList_RejectsNonArray()
    {
        FormValueCodec.TryToClrList(JsonValue.Create("a"), FormFieldType.String, out _, out var error).Should().BeFalse();
        error.Should().Be("ожидается массив значений");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsShapeValid_MultipleRequiresArray(bool multiple)
    {
        var descriptor = new FormFieldDescriptor
        {
            Key = "tags",
            Title = "Теги",
            Type = FormFieldType.String,
            Multiple = multiple,
        };

        FormValueCodec.IsShapeValid(descriptor, new JsonArray("a"), out var error).Should().Be(multiple);
        if (!multiple) error.Should().Be("поле не множественное: ожидается одно значение");
    }

    [Fact]
    public void IsShapeValid_ComputedIsAlwaysValid()
    {
        var descriptor = new FormFieldDescriptor { Key = "back", Title = "Обратная связь", Type = FormFieldType.Computed };

        FormValueCodec.IsShapeValid(descriptor, new JsonArray(1), out var error).Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void IsEmpty_CoversNullEmptyStringAndEmptyArray_ButNotZero()
    {
        FormValueCodec.IsEmpty(null).Should().BeTrue();
        FormValueCodec.IsEmpty(JsonValue.Create("")).Should().BeTrue();
        FormValueCodec.IsEmpty(new JsonArray()).Should().BeTrue();
        FormValueCodec.IsEmpty(JsonValue.Create(0)).Should().BeFalse();
        FormValueCodec.IsEmpty(JsonValue.Create("0")).Should().BeFalse();
    }

    [Fact]
    public void TryToClr_StringTypes_TolerateNumbers()
    {
        FormValueCodec.TryToClr(JsonValue.Create(42), FormFieldType.String, out var value, out _).Should().BeTrue();
        value.Should().Be("42");
    }

    [Fact]
    public void FromClrList_KeepsOrderAsIndex_AndAllowsGaps()
    {
        var node = FormValueCodec.FromClrList(new object?[] { "a", null, "c" }, FormFieldType.String);

        node.Should().BeOfType<JsonArray>().Which.Count.Should().Be(3);
        FormValueCodec.TryToClrList(node, FormFieldType.String, out var values, out _).Should().BeTrue();
        values.Should().HaveCount(3);
        values[0].Should().Be("a");
        values[1].Should().BeNull();
        values[2].Should().Be("c");
    }

    [Fact]
    public void FromClrList_Null_ReturnsNull()
    {
        FormValueCodec.FromClrList(null, FormFieldType.String).Should().BeNull();
    }

    [Fact]
    public void ElementType_OfSelectMany_IsSelect()
    {
        Descriptor(FormFieldType.SelectMany).ElementType.Should().Be(FormFieldType.Select);
        Descriptor(FormFieldType.String).ElementType.Should().Be(FormFieldType.String);
        Descriptor(FormFieldType.Relation).ElementType.Should().Be(FormFieldType.Relation);
    }

    static FormFieldDescriptor Descriptor(FormFieldType type) => new() { Key = "k", Title = "K", Type = type };
}
