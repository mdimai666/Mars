using System.Text.Json.Nodes;
using FluentAssertions;
using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Host.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Mars.Server.Tests.Services;

public class MetaValuesValidatorTests
{
    static MetaFieldDto Field(MetaFieldType type, string key, bool isNullable = true, JsonNode? options = null, bool isMultiple = false)
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
            IsMultiple = isMultiple,
            Default = null,
            Options = options,
            Order = 0,
            Tags = [],
            Hidden = false,
            Disabled = false,
            Variants = null,
            ModelName = null,
        };

    static ModifyMetaValueDetailQuery Value(MetaFieldDto field, string? text = null, int index = 0)
        => new()
        {
            Id = Guid.NewGuid(),
            Index = index,
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

    static MetaValueValidationContext Context(string? model = MetaValueOwnerCatalog.Post, Guid? ownerId = null)
        => new() { ModelName = model, OwnerId = ownerId };

    /// <summary>Валидатор с провайдером уникальности, зарегистрированным под моделью</summary>
    static MetaValuesValidator Validator(MetaValueUniquenessProviderFake? uniqueness = null,
                                         string model = MetaValueOwnerCatalog.Post)
    {
        var serviceProvider = Substitute.For<IKeyedServiceProvider>();
        if (uniqueness is not null)
            serviceProvider.GetKeyedService(typeof(IMetaValueUniquenessProvider), model).Returns(uniqueness);
        return new MetaValuesValidator(serviceProvider);
    }

    static JsonObject RegexOptions(string pattern)
        => new()
        {
            ["validators"] = new JsonArray(new JsonObject
            {
                ["type"] = "regex",
                ["params"] = new JsonObject { ["pattern"] = pattern },
            }),
        };

    static JsonObject UniqueOptions()
        => new()
        {
            ["validators"] = new JsonArray(new JsonObject
            {
                ["type"] = "unique",
                ["params"] = new JsonObject(),
            }),
        };

    [Fact]
    public async Task Validate_RequiredFieldWithoutValue_ReturnsError()
    {
        var field = Field(MetaFieldType.String, "code", isNullable: false);

        var errors = await Validator().ValidateAsync([Value(field, null)], Context());

        errors.Should().ContainSingle().Which.FieldKey.Should().Be("code");
    }

    [Fact]
    public async Task Validate_RegexMismatch_ReturnsError()
    {
        var field = Field(MetaFieldType.String, "code", options: RegexOptions(@"^\d+$"));

        var errors = await Validator().ValidateAsync([Value(field, "abc")], Context());

        errors.Should().ContainSingle();
    }

    [Fact]
    public async Task Validate_RegexMatch_NoErrors()
    {
        var field = Field(MetaFieldType.String, "code", options: RegexOptions(@"^\d+$"));

        var errors = await Validator().ValidateAsync([Value(field, "123")], Context());

        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_StringOutOfRange_ReturnsError()
    {
        var field = Field(MetaFieldType.String, "code") with { MinValue = 3 };

        var errors = await Validator().ValidateAsync([Value(field, "ab")], Context());

        errors.Should().ContainSingle();
    }

    [Fact]
    public async Task Validate_UniqueValueOccupied_ReturnsError()
    {
        var field = Field(MetaFieldType.String, "code", options: UniqueOptions());
        var uniqueness = new MetaValueUniquenessProviderFake { IsOccupiedHandler = (_, _, _) => true };

        var errors = await Validator(uniqueness).ValidateAsync([Value(field, "abc")], Context());

        errors.Should().ContainSingle().Which.Message.Should().Be("значение уже занято");
    }

    [Fact]
    public async Task Validate_SingleFieldSecondIndex_ReturnsError()
    {
        var field = Field(MetaFieldType.String, "code");

        var errors = await Validator().ValidateAsync(
            [Value(field, "a", index: 0), Value(field, "b", index: 1)], Context());

        errors.Should().ContainSingle();
    }

    [Fact]
    public async Task Validate_MultipleFieldSecondIndex_NoErrors()
    {
        var field = Field(MetaFieldType.String, "code", isMultiple: true);

        var errors = await Validator().ValidateAsync(
            [Value(field, "a", index: 0), Value(field, "b", index: 1)], Context());

        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_UniqueValueFree_NoErrors()
    {
        var field = Field(MetaFieldType.String, "code", options: UniqueOptions());

        var errors = await Validator().ValidateAsync([Value(field, "abc")], Context());

        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_UniqueEmptyValue_NotChecked()
    {
        var field = Field(MetaFieldType.String, "code", options: UniqueOptions());
        var uniqueness = new MetaValueUniquenessProviderFake { IsOccupiedHandler = (_, _, _) => true };

        var errors = await Validator(uniqueness).ValidateAsync([Value(field, null)], Context());

        errors.Should().BeEmpty();
        uniqueness.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_UniqueModelWithoutProvider_NotChecked()
    {
        // провайдер домена не зарегистрирован — правило мягко пропускается
        var field = Field(MetaFieldType.String, "code", options: UniqueOptions());
        var uniqueness = new MetaValueUniquenessProviderFake { IsOccupiedHandler = (_, _, _) => true };

        var errors = await Validator(uniqueness, MetaValueOwnerCatalog.Post)
            .ValidateAsync([Value(field, "abc")], Context(model: MetaValueOwnerCatalog.User));

        errors.Should().BeEmpty();
        uniqueness.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_UniquePostCategoryDomain_CheckedByDomainProvider()
    {
        var field = Field(MetaFieldType.String, "code", options: UniqueOptions());
        var uniqueness = new MetaValueUniquenessProviderFake { IsOccupiedHandler = (_, _, _) => true };

        var errors = await Validator(uniqueness, MetaValueOwnerCatalog.PostCategory)
            .ValidateAsync([Value(field, "abc")], Context(model: MetaValueOwnerCatalog.PostCategory));

        errors.Should().ContainSingle();
        uniqueness.Calls.Should().ContainSingle();
    }

    [Fact]
    public async Task Validate_UniqueOwnerIdPassedToProvider()
    {
        var field = Field(MetaFieldType.String, "code", options: UniqueOptions());
        var ownerId = Guid.NewGuid();
        var uniqueness = new MetaValueUniquenessProviderFake();

        await Validator(uniqueness).ValidateAsync([Value(field, "abc")], Context(ownerId: ownerId));

        uniqueness.Calls.Should().ContainSingle()
                  .Which.Should().Be((field.Id, "abc", (Guid?)ownerId));
    }

    [Fact]
    public async Task ValidateJson_MissingRequired_OnlyWhenRequireAll()
    {
        var field = Field(MetaFieldType.String, "code", isNullable: false);
        var validator = Validator();

        (await validator.ValidateJsonAsync([field], null, requireAll: true, Context())).Should().ContainSingle();
        (await validator.ValidateJsonAsync([field], null, requireAll: false, Context())).Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateJson_ContentFieldSkipped()
    {
        // значение поля контента хранится в posts.Content, а не в мета-значениях
        var contentField = Field(MetaFieldType.Text, FeatureFieldsCatalog.ContentFieldKey, isNullable: false);
        var validator = Validator();

        (await validator.ValidateJsonAsync([contentField], null, requireAll: true, Context(), contentFieldKey: contentField.Key))
            .Should().BeEmpty();

        (await validator.ValidateJsonAsync([contentField], null, requireAll: true, Context())).Should().ContainSingle();
    }

    [Fact]
    public async Task ValidateJson_MissingRequiredWithGenerator_NoError()
    {
        // поле с генератором будет заполнено при создании — отсутствие значения не ошибка
        var options = new JsonObject
        {
            ["generator"] = new JsonObject { ["type"] = "sequence" },
        };
        var field = Field(MetaFieldType.String, "code", isNullable: false, options: options);

        (await Validator().ValidateJsonAsync([field], null, requireAll: true, Context())).Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateJson_StringValueCheckedByRegex()
    {
        var field = Field(MetaFieldType.String, "code", options: RegexOptions(@"^\d{3}$"));
        var validator = Validator();

        (await validator.ValidateJsonAsync([field], new Dictionary<string, JsonNode> { ["code"] = "12" }, requireAll: false, Context()))
            .Should().ContainSingle();

        (await validator.ValidateJsonAsync([field], new Dictionary<string, JsonNode> { ["code"] = "123" }, requireAll: false, Context()))
            .Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateJson_UniqueValueOccupied_ReturnsError()
    {
        var field = Field(MetaFieldType.String, "code", options: UniqueOptions());
        var uniqueness = new MetaValueUniquenessProviderFake { IsOccupiedHandler = (_, _, _) => true };

        var errors = await Validator(uniqueness).ValidateJsonAsync(
            [field], new Dictionary<string, JsonNode> { ["code"] = "abc" }, requireAll: false, Context());

        errors.Should().ContainSingle().Which.Message.Should().Be("значение уже занято");
    }

    [Fact]
    public async Task ValidateJson_SingleFieldArrayWithSeveralItems_ReturnsError()
    {
        var field = Field(MetaFieldType.File, "docs");

        var errors = await Validator().ValidateJsonAsync(
            [field],
            new Dictionary<string, JsonNode> { ["docs"] = new JsonArray(Guid.NewGuid().ToString(), Guid.NewGuid().ToString()) },
            requireAll: false, Context());

        errors.Should().ContainSingle();
    }

    [Fact]
    public async Task ValidateJson_MultipleFieldArrayWithSeveralItems_NoMultiplicityError()
    {
        var field = Field(MetaFieldType.File, "docs", isMultiple: true);

        var errors = await Validator().ValidateJsonAsync(
            [field],
            new Dictionary<string, JsonNode> { ["docs"] = new JsonArray(Guid.NewGuid().ToString(), Guid.NewGuid().ToString()) },
            requireAll: false, Context());

        errors.Should().BeEmpty();
    }
}
