using System.Text.Json.Nodes;
using FluentAssertions;
using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Contracts.MetaFields;
using NSubstitute;

namespace Mars.Server.Tests.Dto;

public class MetaFieldsDuplicateQueryValidatorTests
{
    sealed class SupportDto : IGeneralMetaFieldsSupportDto
    {
        public IReadOnlyCollection<MetaFieldDto> MetaFields { get; init; } = [];
    }

    readonly IMetaModelTypesLocator _locator = Substitute.For<IMetaModelTypesLocator>();
    readonly MetaFieldsDuplicateQueryValidator _validator;

    public MetaFieldsDuplicateQueryValidatorTests()
    {
        _locator.ListMetaRelationModelProviderKeys().Returns(["Post", "User", "File", "Feedback", "NavMenu"]);
        _validator = new MetaFieldsDuplicateQueryValidator(_locator);
    }

    static MetaFieldDto Field(MetaFieldType type, string? modelName = null, string? key = null,
                              JsonNode? options = null, bool isMultiple = false)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = "Field",
            Key = key ?? $"key_{Guid.NewGuid():N}",
            Type = type,
            MaxValue = null,
            MinValue = null,
            Description = "",
            IsNullable = true,
            IsMultiple = isMultiple,
            Default = null,
            Options = options,
            Order = 0,
            Tags = [],
            Hidden = false,
            Disabled = false,
            Variants = null,
            ModelName = modelName,
        };

    static JsonObject ListKindOptions() => new() { [MetaFieldKindCatalog.KindOption()] = MetaFieldKindCatalog.List };

    [Theory]
    [InlineData("User")]
    [InlineData("Post")]
    [InlineData("File")]
    public void Validate_RelationWithKnownTargetRoot_ShouldPass(string modelName)
    {
        var dto = new SupportDto { MetaFields = [Field(MetaFieldType.Relation, modelName)] };

        _validator.Validate(dto).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_RelationPostSubtype_ExistType_ShouldPass()
    {
        _locator.ExistPostType("comment").Returns(true);
        var dto = new SupportDto { MetaFields = [Field(MetaFieldType.Relation, "Post.comment")] };

        _validator.Validate(dto).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_RelationPostSubtype_TypeNotExist_ShouldFail()
    {
        _locator.ExistPostType("unknown").Returns(false);
        var dto = new SupportDto { MetaFields = [Field(MetaFieldType.Relation, "Post.unknown")] };

        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("unknown");
    }

    [Fact]
    public void Validate_RelationWithUnknownRoot_ShouldFail()
    {
        var dto = new SupportDto { MetaFields = [Field(MetaFieldType.Relation, "External")] };

        _validator.Validate(dto).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_RelationWithoutModelName_ShouldFail()
    {
        var dto = new SupportDto { MetaFields = [Field(MetaFieldType.Relation, null)] };

        _validator.Validate(dto).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_FileWithModelName_ShouldFail()
    {
        var dto = new SupportDto { MetaFields = [Field(MetaFieldType.File, "User")] };

        _validator.Validate(dto).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_FileWithoutModelName_ShouldPass()
    {
        var dto = new SupportDto { MetaFields = [Field(MetaFieldType.File, null)] };

        _validator.Validate(dto).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ListKindOnMultipleRelationPostTarget_ShouldPass()
    {
        _locator.ExistPostType("photo").Returns(true);
        var dto = new SupportDto { MetaFields = [Field(MetaFieldType.Relation, "Post.photo", options: ListKindOptions(), isMultiple: true)] };

        _validator.Validate(dto).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ListKindOnSingleRelation_ShouldFail()
    {
        var dto = new SupportDto { MetaFields = [Field(MetaFieldType.Relation, "Post.photo", options: ListKindOptions(), isMultiple: false)] };

        _validator.Validate(dto).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ListKindOnNonRelation_ShouldFail()
    {
        var dto = new SupportDto { MetaFields = [Field(MetaFieldType.File, options: ListKindOptions(), isMultiple: true)] };

        _validator.Validate(dto).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ListKindWithNonPostTarget_ShouldFail()
    {
        var dto = new SupportDto { MetaFields = [Field(MetaFieldType.Relation, "User", options: ListKindOptions(), isMultiple: true)] };

        _validator.Validate(dto).IsValid.Should().BeFalse();
    }
}
