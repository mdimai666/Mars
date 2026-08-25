using FluentAssertions;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Host.Shared.Services;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.PostTypes;
using NSubstitute;

namespace Test.Mars.Host.Dto;

public class PostTypeQueryValidatorTests
{
    static MetaFieldDto Field(MetaFieldType type, string key)
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
            IsMultiple = false,
            Default = null,
            Options = null,
            Order = 0,
            Tags = [],
            Hidden = false,
            Disabled = false,
            Variants = [],
            ModelName = null,
        };

    static CreatePostTypeQuery Query(IReadOnlyCollection<string> features, params MetaFieldDto[] fields)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = "Тест",
            TypeName = "test_type",
            Tags = [],
            PostStatusList = [],
            EnabledFeatures = features,
            Disabled = false,
            Visibility = PostTypeVisibility.Public,
            MetaFields = fields,
            ImageFieldKey = null,
        };

    static CreatePostTypeQueryValidator Validator()
        => new(Substitute.For<IMetaModelTypesLocator>());

    [Fact]
    public async Task ContentFeature_NoContentField_Fails()
    {
        var query = Query([PostTypeConstants.Features.Content], Field(MetaFieldType.String, "title"));

        var result = await Validator().ValidateAsync(query);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(query.MetaFields));
    }

    [Fact]
    public async Task ContentFeature_ContentFieldWrongType_Fails()
    {
        var query = Query([PostTypeConstants.Features.Content], Field(MetaFieldType.Int, FeatureFieldsCatalog.ContentFieldKey));

        var result = await Validator().ValidateAsync(query);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(query.MetaFields));
    }

    [Fact]
    public async Task ContentFeature_ContentFieldText_Passes()
    {
        var query = Query([PostTypeConstants.Features.Content], Field(MetaFieldType.Text, FeatureFieldsCatalog.ContentFieldKey));

        var result = await Validator().ValidateAsync(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ContentFeatureDisabled_NoContentField_Passes()
    {
        var query = Query([], Field(MetaFieldType.String, "title"));

        var result = await Validator().ValidateAsync(query);

        result.IsValid.Should().BeTrue();
    }
}
