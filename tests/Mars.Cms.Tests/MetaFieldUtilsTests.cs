using System.Text.Json.Nodes;
using FluentAssertions;
using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Contracts.MetaFields;

namespace Mars.Cms.Tests;

public class MetaFieldUtilsTests
{
    static MetaFieldDto Field(MetaFieldType type, string key = "test_field") => new()
    {
        Id = Guid.NewGuid(),
        Title = "Тестовое поле",
        Key = key,
        Type = type,
        MaxValue = null,
        MinValue = null,
        Description = "",
        IsNullable = true,
        IsMultiple = false,
        Default = null,
        Options = null,
        Order = 0,
        Tags = [],
        Hidden = false,
        Disabled = false,
        Variants = null,
        ModelName = null,
    };

    static ModifyMetaValueDetailQuery Blank(MetaFieldType type)
        => ModifyMetaValueDetailQuery.GetBlank(Field(type));

    /// <summary>Guid-строка как JsonValue из wire-JSON (CLR-строка в GetValue&lt;Guid&gt; не конвертируется)</summary>
    static JsonValue GuidNode(Guid id) => JsonNode.Parse($"\"{id}\"")!.AsValue();

    //===================================== MetaValueFromJson

    [Theory]
    [InlineData(MetaFieldType.Relation)]
    [InlineData(MetaFieldType.File)]
    [InlineData(MetaFieldType.Image)]
    public void FromJson_ReferenceTypes_SetModelId(MetaFieldType type)
    {
        var id = Guid.NewGuid();

        var result = MetaFieldUtils.MetaValueFromJson(Blank(type), GuidNode(id));

        result.ModelId.Should().Be(id);
    }

    [Fact]
    public void FromJson_Scalars_SetTypedColumns()
    {
        MetaFieldUtils.MetaValueFromJson(Blank(MetaFieldType.Bool), JsonValue.Create(true))
            .Bool.Should().BeTrue();

        MetaFieldUtils.MetaValueFromJson(Blank(MetaFieldType.Int), JsonValue.Create(42))
            .Int.Should().Be(42);

        MetaFieldUtils.MetaValueFromJson(Blank(MetaFieldType.Long), JsonValue.Create(5_000_000_000L))
            .Long.Should().Be(5_000_000_000L);

        MetaFieldUtils.MetaValueFromJson(Blank(MetaFieldType.Float), JsonValue.Create(1.5))
            .Float.Should().Be(1.5d);

        MetaFieldUtils.MetaValueFromJson(Blank(MetaFieldType.Decimal), JsonValue.Create(1234.56m))
            .Decimal.Should().Be(1234.56m);

        MetaFieldUtils.MetaValueFromJson(Blank(MetaFieldType.String), JsonValue.Create("короткая"))
            .StringShort.Should().Be("короткая");

        MetaFieldUtils.MetaValueFromJson(Blank(MetaFieldType.Text), JsonValue.Create("длинная"))
            .StringText.Should().Be("длинная");
    }

    [Fact]
    public void FromJson_DateTime_ParsesIsoString()
    {
        var node = JsonNode.Parse("\"2026-09-23T10:30:00\"")!.AsValue();

        var result = MetaFieldUtils.MetaValueFromJson(Blank(MetaFieldType.DateTime), node);

        result.DateTime.Should().Be(new DateTime(2026, 9, 23, 10, 30, 0));
    }

    [Fact]
    public void FromJson_Select_And_SelectMany_SetVariantIds()
    {
        var variantId = Guid.NewGuid();

        MetaFieldUtils.MetaValueFromJson(Blank(MetaFieldType.Select), GuidNode(variantId))
            .VariantId.Should().Be(variantId);

        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
        MetaFieldUtils.MetaValueFromJson(Blank(MetaFieldType.SelectMany), JsonValue.Create(ids)!)
            .VariantsIds.Should().Equal(ids);
    }

    [Fact]
    public void FromJson_Query_Throws()
    {
        var act = () => MetaFieldUtils.MetaValueFromJson(Blank(MetaFieldType.Query), JsonValue.Create("x"));
        act.Should().Throw<NotImplementedException>();
    }

    //===================================== MetaValueFromString

    [Theory]
    [InlineData(MetaFieldType.Relation)]
    [InlineData(MetaFieldType.File)]
    [InlineData(MetaFieldType.Image)]
    public void FromString_ReferenceTypes_SetModelId(MetaFieldType type)
    {
        var id = Guid.NewGuid();

        var result = MetaFieldUtils.MetaValueFromString(Blank(type), id.ToString());

        result.ModelId.Should().Be(id);
    }

    [Fact]
    public void FromString_SelectMany_SplitsCsvGuids()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        var result = MetaFieldUtils.MetaValueFromString(Blank(MetaFieldType.SelectMany), $"{first}, {second}");

        result.VariantsIds.Should().Equal(first, second);
    }

    [Fact]
    public void FromString_BlankValue_KeepsRowUnchanged()
    {
        var blank = Blank(MetaFieldType.String);

        MetaFieldUtils.MetaValueFromString(blank, "  ").Should().Be(blank);
    }

    [Fact]
    public void TryFromString_WrapsFormatErrors()
    {
        var act = () => MetaFieldUtils.TryMetaValueFromString(Blank(MetaFieldType.Int), "не число");

        act.Should().Throw<FormatException>().WithMessage("*не число*Int*");
    }

    //===================================== MetaValueFromObject

    [Theory]
    [InlineData(MetaFieldType.Relation)]
    [InlineData(MetaFieldType.File)]
    [InlineData(MetaFieldType.Image)]
    public void FromObject_ReferenceTypes_SetModelId(MetaFieldType type)
    {
        var id = Guid.NewGuid();

        var result = MetaFieldUtils.MetaValueFromObject(Blank(type), id);

        result.ModelId.Should().Be(id);
    }

    //===================================== GetValueSimple

    [Theory]
    [InlineData(MetaFieldType.Relation)]
    [InlineData(MetaFieldType.File)]
    [InlineData(MetaFieldType.Image)]
    public void GetValueSimple_ReferenceTypes_ReturnModelId(MetaFieldType type)
    {
        var id = Guid.NewGuid();

        var row = MetaFieldUtils.MetaValueFromJson(Blank(type), GuidNode(id));

        row.GetValueSimple().Should().Be(id);
    }
}
