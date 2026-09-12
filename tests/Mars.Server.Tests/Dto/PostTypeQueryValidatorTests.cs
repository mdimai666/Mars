using FluentAssertions;
using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Dto.PostTypes;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Contracts.PostTypes;
using NSubstitute;

namespace Mars.Server.Tests.Dto;

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
    public async Task ContentFeature_NoMetaField_Passes()
    {
        var query = Query([PostTypeConstants.Features.Content], Field(MetaFieldType.String, "title"));

        var result = await Validator().ValidateAsync(query);

        result.IsValid.Should().BeTrue("контент — системный слот, полей типа он не требует");
    }
}
