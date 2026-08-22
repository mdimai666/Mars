using System.Text.Json.Nodes;
using FluentAssertions;
using Mars.Host.Services;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Utils;
using Mars.Shared.Contracts.MetaFields;

namespace Test.Mars.Host.Services;

public class MetaValuesValidatorTests
{
    static MetaFieldDto Field(MetaFieldType type, string key, bool isNullable = true, JsonNode? options = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = key,
            Key = key,
            Type = type,
            MaxValue = null,
            MinValue = null,
            Description = "",
            IsNullable = isNullable,
            Default = null,
            Options = options,
            Order = 0,
            Tags = [],
            Hidden = false,
            Disabled = false,
            Variants = null,
            ModelName = null,
        };

    static ModifyMetaValueDetailQuery Value(MetaFieldDto field, string? text = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Index = 0,
            Bool = null,
            Int = null,
            Float = null,
            Decimal = null,
            Long = null,
            StringText = field.Type == MetaFieldType.Text ? text : null,
            StringShort = field.Type == MetaFieldType.String ? text : null,
            DateTime = null,
            VariantId = null,
            VariantsIds = [],
            ModelId = null,
            MetaFieldId = field.Id,
            MetaField = field,
        };

    static JsonObject RegexOptions(string pattern)
        => new()
        {
            ["validators"] = new JsonArray(new JsonObject
            {
                ["type"] = "regex",
                ["params"] = new JsonObject { ["pattern"] = pattern },
            }),
        };

    [Fact]
    public void Validate_RequiredFieldWithoutValue_ReturnsError()
    {
        var field = Field(MetaFieldType.String, "code", isNullable: false);

        var errors = new MetaValuesValidator().Validate([Value(field, null)]);

        errors.Should().ContainSingle().Which.FieldKey.Should().Be("code");
    }

    [Fact]
    public void Validate_RegexMismatch_ReturnsError()
    {
        var field = Field(MetaFieldType.String, "code", options: RegexOptions(@"^\d+$"));

        var errors = new MetaValuesValidator().Validate([Value(field, "abc")]);

        errors.Should().ContainSingle();
    }

    [Fact]
    public void Validate_RegexMatch_NoErrors()
    {
        var field = Field(MetaFieldType.String, "code", options: RegexOptions(@"^\d+$"));

        var errors = new MetaValuesValidator().Validate([Value(field, "123")]);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_StringOutOfRange_ReturnsError()
    {
        var field = Field(MetaFieldType.String, "code") with { MinValue = 3 };

        var errors = new MetaValuesValidator().Validate([Value(field, "ab")]);

        errors.Should().ContainSingle();
    }

    [Fact]
    public void ValidateJson_MissingRequired_OnlyWhenRequireAll()
    {
        var field = Field(MetaFieldType.String, "code", isNullable: false);
        var validator = new MetaValuesValidator();

        validator.ValidateJson([field], null, requireAll: true).Should().ContainSingle();
        validator.ValidateJson([field], null, requireAll: false).Should().BeEmpty();
    }

    [Fact]
    public void ValidateJson_MissingRequiredWithGenerator_NoError()
    {
        // поле с генератором будет заполнено при создании — отсутствие значения не ошибка
        var options = new JsonObject
        {
            ["generator"] = new JsonObject { ["type"] = "sequence" },
        };
        var field = Field(MetaFieldType.String, "code", isNullable: false, options: options);

        new MetaValuesValidator().ValidateJson([field], null, requireAll: true).Should().BeEmpty();
    }

    [Fact]
    public void ValidateJson_StringValueCheckedByRegex()
    {
        var field = Field(MetaFieldType.String, "code", options: RegexOptions(@"^\d{3}$"));
        var validator = new MetaValuesValidator();

        validator.ValidateJson([field], new Dictionary<string, JsonNode> { ["code"] = "12" }, requireAll: false)
                 .Should().ContainSingle();

        validator.ValidateJson([field], new Dictionary<string, JsonNode> { ["code"] = "123" }, requireAll: false)
                 .Should().BeEmpty();
    }
}
