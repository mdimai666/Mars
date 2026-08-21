using FluentAssertions;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Services;
using Mars.Shared.Contracts.MetaFields;
using NSubstitute;

namespace Test.Mars.Host.Dto;

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

    static MetaFieldDto Field(MetaFieldType type, string? modelName = null, string? key = null)
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
            Default = null,
            Options = null,
            Order = 0,
            Tags = [],
            Hidden = false,
            Disabled = false,
            Variants = null,
            ModelName = modelName,
        };

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
}
