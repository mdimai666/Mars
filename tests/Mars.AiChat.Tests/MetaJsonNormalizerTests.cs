using System.Text.Json.Nodes;
using FluentAssertions;
using Mars.AiChat.Host.Tools;
using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Contracts.MetaFields;

namespace Mars.AiChat.Tests;

public class MetaJsonNormalizerTests
{
    static MetaFieldDto Field(MetaFieldType type, string key = "f", bool multiple = false,
                              params (Guid Id, string Key)[] variants) => new()
    {
        Id = Guid.NewGuid(),
        Title = "Поле",
        Key = key,
        Type = type,
        MaxValue = null,
        MinValue = null,
        Description = "",
        IsNullable = true,
        IsMultiple = multiple,
        Default = null,
        Options = null,
        Order = 0,
        Tags = [],
        Hidden = false,
        Disabled = false,
        Variants = variants.Select(v => new MetaFieldVariantDto
        {
            Id = v.Id,
            Key = v.Key,
            Title = v.Key,
            Tags = [],
            Value = 0,
            Disable = false,
        }).ToList(),
        ModelName = null,
    };

    /// <summary>Полный контур инструмента: текст metaJson → словарь → нормализация</summary>
    static bool Run(string json, MetaFieldDto field, out Dictionary<string, JsonNode> result, out string? error)
    {
        result = [];
        if (!MetaJsonNormalizer.TryParseObject(json, out var meta, out error)) return false;
        return MetaJsonNormalizer.TryNormalize(meta, [field], out result, out error);
    }

    /// <summary>Нормализованный узел принимается JSON-путём CMS (round-trip через MetaValueFromJson)</summary>
    static ModifyMetaValueDetailQuery RoundTrip(MetaFieldDto field, JsonNode node)
        => MetaFieldUtils.MetaValueFromJson(ModifyMetaValueDetailQuery.GetBlank(field), node.AsValue());

    //===================================== TryParseObject

    [Fact]
    public void ParseObject_EmptyText_GivesEmptyDict()
    {
        MetaJsonNormalizer.TryParseObject("", out var meta, out _).Should().BeTrue();
        meta.Should().BeEmpty();

        MetaJsonNormalizer.TryParseObject(null, out var nullMeta, out _).Should().BeTrue();
        nullMeta.Should().BeEmpty();
    }

    [Fact]
    public void ParseObject_RejectsNonObject_AndBrokenJson()
    {
        MetaJsonNormalizer.TryParseObject("[1,2]", out _, out var arrayError).Should().BeFalse();
        arrayError.Should().Contain("объектом");

        MetaJsonNormalizer.TryParseObject("{oops", out _, out var jsonError).Should().BeFalse();
        jsonError.Should().Contain("JSON");
    }

    //===================================== скаляры

    [Fact]
    public void String_PassesThrough_AndStringifiesNonText()
    {
        var field = Field(MetaFieldType.String, "s");

        Run("""{"s":"привет"}""", field, out var text, out _).Should().BeTrue();
        RoundTrip(field, text["s"]!).StringShort.Should().Be("привет");

        Run("""{"s":42}""", field, out var number, out _).Should().BeTrue();
        RoundTrip(field, number["s"]!).StringShort.Should().Be("42");
    }

    [Theory]
    [InlineData("""{"b":true}""", true)]
    [InlineData("""{"b":"false"}""", false)]
    [InlineData("""{"b":1}""", true)]
    [InlineData("""{"b":0}""", false)]
    public void Bool_AcceptsNativeTextAndBit(string json, bool expected)
    {
        var field = Field(MetaFieldType.Bool, "b");

        Run(json, field, out var result, out _).Should().BeTrue();
        RoundTrip(field, result["b"]!).Bool.Should().Be(expected);
    }

    [Fact]
    public void Bool_RejectsGarbage()
    {
        Run("""{"b":"да"}""", Field(MetaFieldType.Bool, "b"), out _, out var error).Should().BeFalse();
        error.Should().Contain("'b'").And.Contain("true");
    }

    [Fact]
    public void Int_AcceptsNumberAndNumericString()
    {
        var field = Field(MetaFieldType.Int, "n");

        Run("""{"n":42}""", field, out var native, out _).Should().BeTrue();
        RoundTrip(field, native["n"]!).Int.Should().Be(42);

        Run("""{"n":"-7"}""", field, out var text, out _).Should().BeTrue();
        RoundTrip(field, text["n"]!).Int.Should().Be(-7);
    }

    [Fact]
    public void Int_RejectsFractional_AndOverflow_AndGarbage()
    {
        var field = Field(MetaFieldType.Int, "n");

        Run("""{"n":12.5}""", field, out _, out var fraction).Should().BeFalse();
        fraction.Should().Contain("целое");

        Run("""{"n":3000000000}""", field, out _, out var overflow).Should().BeFalse();
        overflow.Should().Contain("Int32");

        Run("""{"n":"abc"}""", field, out _, out var garbage).Should().BeFalse();
        garbage.Should().Contain("'n'");
    }

    [Fact]
    public void Long_AcceptsBigNumbers()
    {
        var field = Field(MetaFieldType.Long, "n");

        Run("""{"n":"5000000000"}""", field, out var result, out _).Should().BeTrue();
        RoundTrip(field, result["n"]!).Long.Should().Be(5_000_000_000L);
    }

    [Fact]
    public void Float_And_Decimal_AcceptNumberAndString()
    {
        var floatField = Field(MetaFieldType.Float, "f");
        Run("""{"f":1.5}""", floatField, out var floatNative, out _).Should().BeTrue();
        RoundTrip(floatField, floatNative["f"]!).Float.Should().Be(1.5d);
        Run("""{"f":"2.25"}""", floatField, out var floatText, out _).Should().BeTrue();
        RoundTrip(floatField, floatText["f"]!).Float.Should().Be(2.25d);

        var decimalField = Field(MetaFieldType.Decimal, "d");
        Run("""{"d":"1234.56"}""", decimalField, out var decimalText, out _).Should().BeTrue();
        RoundTrip(decimalField, decimalText["d"]!).Decimal.Should().Be(1234.56m);
    }

    [Fact]
    public void DateTime_PassesIsoString_RejectsGarbage()
    {
        var field = Field(MetaFieldType.DateTime, "dt");

        Run("""{"dt":"2026-09-23T10:30:00"}""", field, out var result, out _).Should().BeTrue();
        RoundTrip(field, result["dt"]!).DateTime.Should().Be(new DateTime(2026, 9, 23, 10, 30, 0));

        Run("""{"dt":"вчера"}""", field, out _, out var error).Should().BeFalse();
        error.Should().Contain("ISO-8601");
    }

    //===================================== ссылки

    [Fact]
    public void Image_AcceptsGuidString_RejectsOtherText()
    {
        var field = Field(MetaFieldType.Image, "img");
        var id = Guid.NewGuid();

        Run($$"""{"img":"{{id}}"}""", field, out var result, out _).Should().BeTrue();
        RoundTrip(field, result["img"]!).ModelId.Should().Be(id);

        Run("""{"img":"картинка.png"}""", field, out _, out var error).Should().BeFalse();
        error.Should().Contain("'img'").And.Contain("ListMedia");
    }

    //===================================== выбор вариантов

    [Fact]
    public void Select_ResolvesVariantKey_PassesKnownGuid_RejectsUnknown()
    {
        var hot = Guid.NewGuid();
        var field = Field(MetaFieldType.Select, "sel", variants: [(hot, "hot"), (Guid.NewGuid(), "cold")]);

        Run("""{"sel":"hot"}""", field, out var byKey, out _).Should().BeTrue();
        RoundTrip(field, byKey["sel"]!).VariantId.Should().Be(hot);

        Run($$"""{"sel":"{{hot}}"}""", field, out var byGuid, out _).Should().BeTrue();
        RoundTrip(field, byGuid["sel"]!).VariantId.Should().Be(hot);

        Run("""{"sel":"warm"}""", field, out _, out var error).Should().BeFalse();
        error.Should().Contain("'warm'").And.Contain("hot").And.Contain("cold");
    }

    [Fact]
    public void SelectMany_AcceptsKeyArray_AndCsv_ProducesGuidArrayNode()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var field = Field(MetaFieldType.SelectMany, "many", variants: [(a, "a"), (b, "b")]);

        Run("""{"many":["a","b"]}""", field, out var fromArray, out _).Should().BeTrue();
        RoundTrip(field, fromArray["many"]!).VariantsIds.Should().Equal(a, b);

        Run("""{"many":"b, a"}""", field, out var fromCsv, out _).Should().BeTrue();
        RoundTrip(field, fromCsv["many"]!).VariantsIds.Should().Equal(b, a);
    }

    [Fact]
    public void SelectMany_RejectsUnknownKey()
    {
        var field = Field(MetaFieldType.SelectMany, "many", variants: [(Guid.NewGuid(), "a")]);

        Run("""{"many":["a","x"]}""", field, out _, out var error).Should().BeFalse();
        error.Should().Contain("'many'").And.Contain("'x'");
    }

    //===================================== множественные поля

    [Fact]
    public void Multiple_Image_AcceptsArrayAndSingleValue()
    {
        var field = Field(MetaFieldType.Image, "imgs", multiple: true);
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        Run($$"""{"imgs":["{{first}}","{{second}}"]}""", field, out var fromArray, out _).Should().BeTrue();
        var array = fromArray["imgs"]!.AsArray();
        array.Should().HaveCount(2);
        RoundTrip(field, array[0]!).ModelId.Should().Be(first);
        RoundTrip(field, array[1]!).ModelId.Should().Be(second);

        Run($$"""{"imgs":"{{first}}"}""", field, out var fromSingle, out _).Should().BeTrue();
        fromSingle["imgs"]!.AsArray().Should().HaveCount(1);
    }

    [Fact]
    public void Multiple_String_SplitsCsv()
    {
        var field = Field(MetaFieldType.String, "strs", multiple: true);

        Run("""{"strs":"один, два ,три"}""", field, out var result, out _).Should().BeTrue();
        var array = result["strs"]!.AsArray();
        array.Select(n => RoundTrip(field, n!).StringShort).Should().Equal("один", "два", "три");
    }

    [Fact]
    public void SingleValueField_RejectsArray()
    {
        Run("""{"s":["a","b"]}""", Field(MetaFieldType.String, "s"), out _, out var error).Should().BeFalse();
        error.Should().Contain("'s'").And.Contain("одиночное");
    }

    [Fact]
    public void Multiple_ReportsElementIndex()
    {
        var field = Field(MetaFieldType.Int, "nums", multiple: true);

        Run("""{"nums":[1,"oops"]}""", field, out _, out var error).Should().BeFalse();
        error.Should().Contain("'nums'").And.Contain("элемент 1");
    }

    //===================================== прочее

    [Fact]
    public void UnknownKey_ListsValidFields()
    {
        Run("""{"nope":1}""", Field(MetaFieldType.Int, "known"), out _, out var error).Should().BeFalse();
        error.Should().Contain("'nope'").And.Contain("known");
    }

    [Fact]
    public void QueryFields_AndNullValues_AreSkipped()
    {
        var fields = new[]
        {
            Field(MetaFieldType.String, "s"),
            Field(MetaFieldType.Query, "q"),
        };

        MetaJsonNormalizer.TryParseObject("""{"s":null,"q":"что-то"}""", out var meta, out _).Should().BeTrue();
        MetaJsonNormalizer.TryNormalize(meta, fields, out var result, out var error).Should().BeTrue();

        error.Should().BeNull();
        result.Should().BeEmpty();
    }
}
