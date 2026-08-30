using System.Text.Json.Nodes;
using FluentAssertions;
using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Dto.PostTypes;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Contracts.PostTypes;
using Mars.Cms.Host.Handlers;
using Mars.Core.Exceptions;
using NSubstitute;

namespace Test.Mars.Server.Handlers;

public class MetaValueGeneratorHandlersTests
{
    static readonly DateTimeOffset _now = new(2026, 8, 23, 10, 0, 0, TimeSpan.FromHours(9));

    static MetaFieldDto Field(MetaFieldType type, string key, JsonNode? options = null)
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
            Options = options,
            Order = 0,
            Tags = [],
            Hidden = false,
            Disabled = false,
            Variants = null,
            ModelName = null,
        };

    static PostTypeDetail PostType(params MetaFieldDto[] fields)
        => new()
        {
            Id = Guid.NewGuid(),
            CreatedAt = _now,
            Title = "post",
            TypeName = "post",
            Tags = [],
            EnabledFeatures = [],
            Disabled = false,
            Visibility = PostTypeVisibility.Public,
            ModifiedAt = null,
            PostStatusList = [],
            MetaFields = fields,
            Presentation = PostTypePresentation.Default(),
        };

    static MetaValueGeneratorContext Context(MetaFieldDto field, IReadOnlyList<string>? categorySlugs = null)
        => new(PostType(field), field, categorySlugs ?? [], _now);

    [Fact]
    public async Task Sequence_PrefixWithPadding()
    {
        var field = Field(MetaFieldType.String, "number");
        var sequenceRepository = Substitute.For<IMetaSequenceRepository>();
        sequenceRepository.NextValueAsync(field.Id, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(2);

        var handler = new SequenceValueGeneratorHandler(sequenceRepository);
        var parameters = new JsonObject { ["prefix"] = "ВУ", ["paddingWidth"] = 4 };

        var value = await handler.GenerateAsync(Context(field), parameters, CancellationToken.None);

        value.Should().Be("ВУ0002");
    }

    [Fact]
    public async Task Sequence_DailyMode_ScopeIncludesDate()
    {
        var field = Field(MetaFieldType.String, "number");
        var sequenceRepository = Substitute.For<IMetaSequenceRepository>();
        sequenceRepository.NextValueAsync(field.Id, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(1);

        var handler = new SequenceValueGeneratorHandler(sequenceRepository);
        var parameters = new JsonObject
        {
            ["prefix"] = "ВУ",
            ["mode"] = MetaFieldGeneratorCatalog.ModeDaily,
        };

        await handler.GenerateAsync(Context(field), parameters, CancellationToken.None);

        await sequenceRepository.Received().NextValueAsync(field.Id, $"ВУ|{_now:yyyy-MM-dd}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sequence_CategoryPrefixFromDictionary()
    {
        var field = Field(MetaFieldType.String, "number");
        var sequenceRepository = Substitute.For<IMetaSequenceRepository>();
        sequenceRepository.NextValueAsync(field.Id, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(7);

        var handler = new SequenceValueGeneratorHandler(sequenceRepository);
        var parameters = new JsonObject
        {
            ["prefix"] = "ОБЩ",
            ["paddingWidth"] = 3,
            ["categoryPrefixes"] = new JsonObject { ["vuz"] = "ВУ" },
        };

        var value = await handler.GenerateAsync(Context(field, ["school", "vuz"]), parameters, CancellationToken.None);

        value.Should().Be("ВУ007");
        await sequenceRepository.Received().NextValueAsync(field.Id, "ВУ", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sequence_NoCategoryMatch_UsesDefaultPrefix()
    {
        var field = Field(MetaFieldType.String, "number");
        var sequenceRepository = Substitute.For<IMetaSequenceRepository>();
        sequenceRepository.NextValueAsync(field.Id, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(1);

        var handler = new SequenceValueGeneratorHandler(sequenceRepository);
        var parameters = new JsonObject
        {
            ["prefix"] = "ОБЩ",
            ["categoryPrefixes"] = new JsonObject { ["vuz"] = "ВУ" },
        };

        var value = await handler.GenerateAsync(Context(field, ["school"]), parameters, CancellationToken.None);

        value.Should().Be("ОБЩ1");
    }

    [Fact]
    public async Task Sequence_NotStringField_Throws()
    {
        var field = Field(MetaFieldType.DateTime, "number");
        var handler = new SequenceValueGeneratorHandler(Substitute.For<IMetaSequenceRepository>());

        var act = () => handler.GenerateAsync(Context(field), [], CancellationToken.None);

        await act.Should().ThrowAsync<MarsValidationException>();
    }

    [Fact]
    public async Task Now_ReturnsMomentOfCreation()
    {
        var field = Field(MetaFieldType.DateTime, "issued");

        var value = await new NowValueGeneratorHandler().GenerateAsync(Context(field), null, CancellationToken.None);

        value.Should().Be(_now.DateTime);
    }

    [Fact]
    public async Task Now_NotDateTimeField_Throws()
    {
        var field = Field(MetaFieldType.String, "issued");

        var act = () => new NowValueGeneratorHandler().GenerateAsync(Context(field), null, CancellationToken.None);

        await act.Should().ThrowAsync<MarsValidationException>();
    }
}
