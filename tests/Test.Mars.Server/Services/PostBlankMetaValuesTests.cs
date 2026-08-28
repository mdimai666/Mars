using FluentAssertions;
using Mars.Cms.Abstractions.Utils;
using Mars.Cms.Host.Services;
using Mars.Cms.Abstractions;
using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Contracts.MetaFields;

namespace Test.Mars.Server.Services;

public class PostBlankMetaValuesTests
{
    static MetaFieldDto Field(MetaFieldType type, string key, bool isMultiple = false)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = key,
            Key = key,
            Type = type,
            MaxValue = null,
            MinValue = null,
            Description = "",
            IsNullable = true,
            IsMultiple = isMultiple,
            Default = null,
            Options = null,
            Order = 0,
            Tags = [],
            Hidden = false,
            Disabled = false,
            Variants = null,
            ModelName = null,
        };

    [Fact]
    public void Enrich_MultipleFieldWithoutValues_NoBlankRows()
    {
        var fields = new[] { Field(MetaFieldType.Relation, "docs", isMultiple: true) };

        var result = MetaValuesEnricher.EnrichWithBlankMetaValuesFromMetaValues([], fields);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Enrich_SingleFieldWithoutValues_OneBlankRow()
    {
        var fields = new[] { Field(MetaFieldType.String, "code") };

        var result = MetaValuesEnricher.EnrichWithBlankMetaValuesFromMetaValues([], fields);

        result.Should().ContainSingle().Which.MetaField.Key.Should().Be("code");
    }

    [Fact]
    public void Enrich_MultipleFieldWithExistingRows_RowsKeptWithoutBlank()
    {
        var field = Field(MetaFieldType.File, "docs", isMultiple: true);
        var existing = new[]
        {
            new MetaValueDetailDto
            {
                Id = Guid.NewGuid(),
                Index = 0,
                Bool = null, Int = null, Float = null, Decimal = null, Long = null,
                StringText = null, StringShort = null, DateTime = null,
                VariantId = null, VariantsIds = [], ModelId = Guid.NewGuid(),
                MetaField = field,
            },
        };

        var result = MetaValuesEnricher.EnrichWithBlankMetaValuesFromMetaValues(existing, [field]);

        result.Should().ContainSingle().Which.Id.Should().Be(existing[0].Id);
    }
}
