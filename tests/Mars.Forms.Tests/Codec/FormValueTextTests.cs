using System.Text.Json.Nodes;
using FluentAssertions;
using Mars.Forms.Contracts;

namespace Mars.Forms.Tests.Codec;

public class FormValueTextTests
{
    static FormFieldDescriptor Field(FormFieldType type, bool multiple = false,
                                     params (string Key, string Title)[] choices) => new()
    {
        Key = "test_field",
        Title = "Тестовое поле",
        Type = type,
        Multiple = multiple,
        Choices = choices.Select(c => new FormChoiceOption { Key = c.Key, Title = c.Title }).ToList(),
    };

    //===================================== одиночные значения

    [Theory]
    [InlineData(FormFieldType.String, "  hello world  ")]
    [InlineData(FormFieldType.Text, "многострочный\nтекст")]
    public void String_And_Text_PassThrough_AsIs(FormFieldType type, string raw)
    {
        FormValueText.TryToClr(raw, Field(type), out var value, out _).Should().BeTrue();
        value.Should().Be(raw);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    [InlineData(" false ", false)]
    public void Bool_ParsesTrueFalse_CaseInsensitive(string raw, bool expected)
    {
        FormValueText.TryToClr(raw, Field(FormFieldType.Bool), out var value, out _).Should().BeTrue();
        value.Should().Be(expected);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("да")]
    [InlineData("")]
    public void Bool_RejectsNonBooleanText_ExceptEmpty(string raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            FormValueText.TryToClr(raw, Field(FormFieldType.Bool), out var value, out _).Should().BeTrue();
            value.Should().BeNull();
            return;
        }

        FormValueText.TryToClr(raw, Field(FormFieldType.Bool), out _, out var error).Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(FormFieldType.Int, "42", 42)]
    [InlineData(FormFieldType.Int, "-7", -7)]
    [InlineData(FormFieldType.Long, "5000000000", 5000000000L)]
    public void IntegerTypes_ParseInvariantCulture(FormFieldType type, string raw, object expected)
    {
        FormValueText.TryToClr(raw, Field(type), out var value, out _).Should().BeTrue();
        value.Should().Be(expected);
    }

    [Fact]
    public void Int_Overflow_IsRejectedByCodec()
    {
        FormValueText.TryToClr("5000000000", Field(FormFieldType.Int), out _, out var error).Should().BeFalse();
        error.Should().Contain("Int32");
    }

    [Fact]
    public void Float_And_Decimal_ParseInvariantCulture()
    {
        FormValueText.TryToClr("1.5", Field(FormFieldType.Float), out var floating, out _).Should().BeTrue();
        floating.Should().Be(1.5d);

        FormValueText.TryToClr("1234.56", Field(FormFieldType.Decimal), out var money, out _).Should().BeTrue();
        money.Should().Be(1234.56m);
    }

    [Fact]
    public void Number_RejectsGarbage()
    {
        FormValueText.TryToClr("не число", Field(FormFieldType.Float), out _, out var error).Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void DateTime_ParsesIso8601_WithOffset()
    {
        FormValueText.TryToClr("2026-09-23T10:30:00+09:00", Field(FormFieldType.DateTime), out var value, out _)
            .Should().BeTrue();

        var date = value.Should().BeOfType<DateTimeOffset>().Subject;
        date.UtcDateTime.Should().Be(new DateTime(2026, 9, 23, 1, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void DateTime_ParsesDateOnly()
    {
        FormValueText.TryToClr("2026-09-23", Field(FormFieldType.DateTime), out var value, out _).Should().BeTrue();
        value.Should().BeOfType<DateTimeOffset>().Subject.Date.Should().Be(new DateTime(2026, 9, 23));
    }

    [Fact]
    public void DateTime_RejectsGarbage()
    {
        FormValueText.TryToClr("вчера", Field(FormFieldType.DateTime), out _, out var error).Should().BeFalse();
        error.Should().Contain("ISO-8601");
    }

    [Fact]
    public void Image_ParsesGuid_AndRejectsOtherText()
    {
        var id = Guid.NewGuid();

        FormValueText.TryToClr(id.ToString(), Field(FormFieldType.Image), out var value, out _).Should().BeTrue();
        value.Should().Be(id);

        FormValueText.TryToClr("картинка.png", Field(FormFieldType.Image), out _, out var error).Should().BeFalse();
        error.Should().Contain("Guid");
    }

    //===================================== выбор вариантов

    [Fact]
    public void Select_PassesVariantKey_AndRejectsUnknown()
    {
        var field = Field(FormFieldType.Select, choices: [("draft", "Черновик"), ("hot", "Горячее")]);

        FormValueText.TryToClr("hot", field, out var value, out _).Should().BeTrue();
        value.Should().Be("hot");

        FormValueText.TryToClr("cold", field, out _, out var error).Should().BeFalse();
        error.Should().Contain("draft").And.Contain("hot");
    }

    [Fact]
    public void SelectMany_AcceptsJsonArray_AndCsv()
    {
        var field = Field(FormFieldType.SelectMany, choices: [("a", "A"), ("b", "B"), ("c", "C")]);

        FormValueText.TryToClr("[\"a\",\"c\"]", field, out var fromJson, out _).Should().BeTrue();
        fromJson.Should().BeEquivalentTo(new[] { "a", "c" });

        FormValueText.TryToClr("a, c", field, out var fromCsv, out _).Should().BeTrue();
        fromCsv.Should().BeEquivalentTo(new[] { "a", "c" });
    }

    [Fact]
    public void SelectMany_RejectsUnknownVariantKey()
    {
        var field = Field(FormFieldType.SelectMany, choices: [("a", "A")]);

        FormValueText.TryToClr("a,x", field, out _, out var error).Should().BeFalse();
        error.Should().Contain("'x'");
    }

    //===================================== множественные поля

    [Fact]
    public void Multiple_Image_AcceptsJsonArrayOfGuids()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        FormValueText.TryToClr($"[\"{first}\",\"{second}\"]", Field(FormFieldType.Image, multiple: true),
            out var value, out _).Should().BeTrue();

        value.Should().BeAssignableTo<IReadOnlyList<object?>>()!
            .Which.Should().Equal(first, second);
    }

    [Fact]
    public void Multiple_Image_AcceptsCsv()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        FormValueText.TryToClr($"{first},{second}", Field(FormFieldType.Image, multiple: true),
            out var value, out _).Should().BeTrue();

        value.Should().BeAssignableTo<IReadOnlyList<object?>>()!
            .Which.Should().Equal(first, second);
    }

    [Fact]
    public void Multiple_String_SplitsCsv_AndKeepsJsonArrayTexts()
    {
        FormValueText.TryToClr("один, два ,три", Field(FormFieldType.String, multiple: true),
            out var fromCsv, out _).Should().BeTrue();
        fromCsv.Should().BeAssignableTo<IReadOnlyList<object?>>()!
            .Which.Should().Equal("один", "два", "три");

        // запятые внутри значений — только JSON-массивом
        FormValueText.TryToClr("[\"раз,два\",\"три\"]", Field(FormFieldType.String, multiple: true),
            out var fromJson, out _).Should().BeTrue();
        fromJson.Should().BeAssignableTo<IReadOnlyList<object?>>()!
            .Which.Should().Equal("раз,два", "три");
    }

    [Fact]
    public void Multiple_Number_ParsesEachCsvPart()
    {
        FormValueText.TryToClr("1, 2, 3", Field(FormFieldType.Int, multiple: true),
            out var value, out _).Should().BeTrue();

        value.Should().BeAssignableTo<IReadOnlyList<object?>>()!
            .Which.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void Multiple_ReportsErrorWithElementIndex()
    {
        // JSON-массив — строгая каноническая форма (числа числами); CSV — терпимый текстовый путь
        FormValueText.TryToClr("[1,\"oops\"]", Field(FormFieldType.Int, multiple: true),
            out _, out var error).Should().BeFalse();

        error.Should().Contain("элемент 1");
    }

    [Fact]
    public void EmptyText_ClearsValue()
    {
        FormValueText.TryToClr("", Field(FormFieldType.String), out var single, out _).Should().BeTrue();
        single.Should().BeNull();

        FormValueText.TryToClr("   ", Field(FormFieldType.Image, multiple: true), out var list, out _).Should().BeTrue();
        list.Should().BeAssignableTo<IReadOnlyList<object?>>().Which.Should().BeEmpty();
    }

    //===================================== прочее

    [Fact]
    public void Computed_Field_IsRejected()
    {
        FormValueText.TryToClr("что угодно", Field(FormFieldType.Computed), out _, out var error).Should().BeFalse();
        error.Should().Contain("вычислимое");
    }

    [Fact]
    public void Object_PassesThroughParsedJson()
    {
        FormValueText.TryToClr("{\"a\":1}", Field(FormFieldType.Object), out var value, out _).Should().BeTrue();

        var node = value.Should().BeOfType<JsonObject>().Subject;
        node["a"]!.GetValue<int>().Should().Be(1);
    }

    [Fact]
    public void Object_RejectsBrokenJson()
    {
        FormValueText.TryToClr("{a:1", Field(FormFieldType.Object), out _, out var error).Should().BeFalse();
        error.Should().Contain("JSON");
    }

    [Fact]
    public void TryToNode_ProducesCanonicalWireForm()
    {
        // decimal — строкой, дата — ISO "O", Guid — формат "D" (канон FormValueCodec)
        FormValueText.TryToNode("1234.5", FormFieldType.Decimal, out var money, out _).Should().BeTrue();
        money!.GetValue<string>().Should().Be("1234.5");

        FormValueText.TryToNode("2026-09-23T00:00:00Z", FormFieldType.DateTime, out var date, out _).Should().BeTrue();
        date!.GetValue<string>().Should().Be("2026-09-23T00:00:00.0000000+00:00");

        FormValueText.TryToNode(null, FormFieldType.String, out var empty, out _).Should().BeTrue();
        empty.Should().BeNull();
    }
}
