using System.Text.Json.Nodes;
using FluentAssertions;
using Mars.Core.Exceptions;
using Mars.Host.Handlers;
using Mars.Host.Services;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.PostCategories;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.Posts;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Test.Mars.Host.Services;

public class MetaValuesGeneratorServiceTests
{
    static readonly DateTimeOffset _now = DateTimeOffset.Now;

    static MetaFieldDto Field(MetaFieldType type, string key, JsonNode? options = null, bool isNullable = true, decimal? minValue = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = key,
            Key = key,
            Type = type,
            MaxValue = null,
            MinValue = minValue,
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
            ModifiedAt = null,
            PostStatusList = [],
            PostContentSettings = new PostContentSettingsDto { PostContentType = "plain", CodeLang = null },
            MetaFields = fields,
            Presentation = PostTypePresentation.Default(),
        };

    static CreatePostQuery Query(params ModifyMetaValueDetailQuery[] values)
        => new()
        {
            Title = "t",
            Type = "post",
            Slug = "s",
            Tags = [],
            UserId = Guid.NewGuid(),
            Status = null,
            Content = null,
            Excerpt = null,
            LangCode = "",
            CategoryIds = [],
            MetaValues = values,
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

    static JsonObject SequenceOptions(string prefix = "ВУ", int paddingWidth = 4)
        => new()
        {
            ["generator"] = new JsonObject
            {
                ["type"] = MetaFieldGeneratorCatalog.Sequence,
                ["params"] = new JsonObject { ["prefix"] = prefix, ["paddingWidth"] = paddingWidth },
            },
        };

    static (MetaValuesGeneratorService service, IMetaSequenceRepository sequenceRepository) Service(
        MetaFieldDto field,
        IPostRepository? postRepository = null,
        IMetaModelTypesLocator? metaModelTypesLocator = null)
    {
        var sequenceRepository = Substitute.For<IMetaSequenceRepository>();
        sequenceRepository.NextValueAsync(field.Id, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(1);

        var handler = new SequenceValueGeneratorHandler(sequenceRepository);
        var serviceProvider = Substitute.For<IKeyedServiceProvider>();
        serviceProvider.GetKeyedService(typeof(IMetaValueGeneratorHandler), MetaFieldGeneratorCatalog.Sequence).Returns(handler);

        var categoryRepository = Substitute.For<IPostCategoryRepository>();
        categoryRepository.ListAll(Arg.Any<ListAllPostCategoryQuery>(), Arg.Any<CancellationToken>())
                          .Returns(Task.FromResult<IReadOnlyCollection<PostCategorySummary>>([]));

        var service = new MetaValuesGeneratorService(
            serviceProvider,
            categoryRepository,
            new MetaValuesValidator(),
            postRepository ?? Substitute.For<IPostRepository>(),
            sequenceRepository,
            metaModelTypesLocator ?? Substitute.For<IMetaModelTypesLocator>());

        return (service, sequenceRepository);
    }

    [Fact]
    public async Task ApplyAsync_NoValue_GeneratesAndAdds()
    {
        var field = Field(MetaFieldType.String, "number", SequenceOptions());
        var (service, _) = Service(field);

        var result = await service.ApplyAsync(PostType(field), Query(), CancellationToken.None);

        result.MetaValues.Should().ContainSingle()
              .Which.StringShort.Should().Be("ВУ0001");
    }

    [Fact]
    public async Task ApplyAsync_BlankValue_Replaced()
    {
        var field = Field(MetaFieldType.String, "number", SequenceOptions());
        var (service, _) = Service(field);

        var result = await service.ApplyAsync(PostType(field), Query(Value(field, "")), CancellationToken.None);

        result.MetaValues.Should().ContainSingle()
              .Which.StringShort.Should().Be("ВУ0001");
    }

    [Fact]
    public async Task ApplyAsync_ExplicitValue_Preserved()
    {
        var field = Field(MetaFieldType.String, "number", SequenceOptions());
        var (service, sequenceRepository) = Service(field);

        var result = await service.ApplyAsync(PostType(field), Query(Value(field, "abc")), CancellationToken.None);

        result.MetaValues.Should().ContainSingle()
              .Which.StringShort.Should().Be("abc");
        await sequenceRepository.DidNotReceive().NextValueAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyAsync_UnknownGenerator_Throws()
    {
        var field = Field(MetaFieldType.String, "number", new JsonObject
        {
            ["generator"] = new JsonObject { ["type"] = "bogus" },
        });
        var (service, _) = Service(field);

        var act = () => service.ApplyAsync(PostType(field), Query(), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<MarsValidationException>();
        exception.Which.Errors.Values.SelectMany(s => s).Should().Contain(s => s.Contains("bogus"));
    }

    [Fact]
    public async Task ApplyAsync_NoGeneratorFields_QueryUnchanged()
    {
        var field = Field(MetaFieldType.String, "number");
        var (service, _) = Service(field);
        var query = Query(Value(field, "abc"));

        var result = await service.ApplyAsync(PostType(field), query, CancellationToken.None);

        result.Should().BeSameAs(query);
    }

    [Fact]
    public async Task ApplyAsync_GeneratedValueAgainstFieldValidator_Throws()
    {
        // длина «ВУ0001» = 6 < MinValue 10 — генератор конфликтует с ограничителем поля
        var field = Field(MetaFieldType.String, "number", SequenceOptions(), minValue: 10);
        var (service, _) = Service(field);

        var act = () => service.ApplyAsync(PostType(field), Query(), CancellationToken.None);

        await act.Should().ThrowAsync<MarsValidationException>();
    }

    static PostDetail Post(MetaFieldDto field, DateTimeOffset createdAt, object? value = null, KeyValuePair<string, string>? status = null)
        => new()
        {
            Id = Guid.NewGuid(),
            CreatedAt = createdAt,
            ModifiedAt = null,
            Title = "t",
            Type = "post",
            Slug = "s",
            Tags = [],
            Author = new PostAuthor { Id = Guid.NewGuid(), UserName = "u", DisplayName = "u" },
            Status = status,
            Categories = null,
            Content = null,
            MetaValues = value is null
                ? new Dictionary<string, MetaValueDto>()
                : new Dictionary<string, MetaValueDto>
                {
                    [field.Key] = new MetaValueDto
                    {
                        Id = Guid.NewGuid(),
                        Type = field.Type,
                        Index = 0,
                        VariantId = null,
                        VariantsIds = null,
                        ModelId = null,
                        Value = value,
                        MetaField = field,
                    },
                },
        };

    static (MetaValuesGeneratorService service, IMetaSequenceRepository sequenceRepository, IPostRepository postRepository) RegenerationService(
        MetaFieldDto field, IReadOnlyCollection<PostDetail> posts)
    {
        var postRepository = Substitute.For<IPostRepository>();
        postRepository.ListAllDetail(Arg.Any<ListAllPostQuery>(), Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult<IReadOnlyCollection<PostDetail>>(posts));

        var locator = Substitute.For<IMetaModelTypesLocator>();
        locator.GetPostTypeByName("post").Returns(PostType(field));

        var (service, sequenceRepository) = Service(field, postRepository, locator);
        return (service, sequenceRepository, postRepository);
    }

    [Fact]
    public async Task RegenerateAsync_AllMode_RenumbersInCreationOrder_FixesCounter()
    {
        var field = Field(MetaFieldType.String, "number", SequenceOptions());
        var day = DateTimeOffset.Now.AddDays(-3);
        var posts = new[]
        {
            Post(field, day.AddDays(2)),
            Post(field, day),
            Post(field, day.AddDays(1)),
        };
        var (service, sequenceRepository, postRepository) = RegenerationService(field, posts);
        IReadOnlyCollection<PostMetaValueUpsert>? upserts = null;
        postRepository.UpsertMetaValuesAsync(
                Arg.Do<IReadOnlyCollection<PostMetaValueUpsert>>(u => upserts = u), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await service.RegenerateAsync(
            new RegenerateMetaValuesQuery { PostTypeName = "post", Mode = MetaValuesRegenerationMode.All }, CancellationToken.None);

        result.Should().Be(new RegenerateMetaValuesResult(3, 3));
        upserts.Should().NotBeNull();
        upserts!.Select(u => u.Value).Should().ContainInOrder("ВУ0001", "ВУ0002", "ВУ0003");
        await sequenceRepository.Received().SetValueAsync(field.Id, "ВУ", 3, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegenerateAsync_FromLast_FillsEmptyOnly_ContinuesCounter()
    {
        var field = Field(MetaFieldType.String, "number", SequenceOptions());
        var day = DateTimeOffset.Now.AddDays(-1);
        var posts = new[]
        {
            Post(field, day, value: "ВУ0001"),
            Post(field, day.AddHours(1)),
        };
        var (service, sequenceRepository, postRepository) = RegenerationService(field, posts);
        sequenceRepository.NextValueAsync(field.Id, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(2);
        IReadOnlyCollection<PostMetaValueUpsert>? upserts = null;
        postRepository.UpsertMetaValuesAsync(
                Arg.Do<IReadOnlyCollection<PostMetaValueUpsert>>(u => upserts = u), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await service.RegenerateAsync(
            new RegenerateMetaValuesQuery { PostTypeName = "post", Mode = MetaValuesRegenerationMode.FromLast }, CancellationToken.None);

        result.Should().Be(new RegenerateMetaValuesResult(2, 1));
        upserts.Should().NotBeNull();
        upserts!.Should().ContainSingle();
        upserts!.Single().PostId.Should().Be(posts[1].Id);
        upserts.Single().Value.Should().Be("ВУ0002");
        await sequenceRepository.DidNotReceive().SetValueAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegenerateAsync_StatusFilter_OnlyMatchingPosts()
    {
        var field = Field(MetaFieldType.String, "number", SequenceOptions());
        var day = DateTimeOffset.Now.AddDays(-1);
        var posts = new[]
        {
            Post(field, day, status: new KeyValuePair<string, string>("published", "Опубликован")),
            Post(field, day.AddHours(1), status: new KeyValuePair<string, string>("draft", "Черновик")),
        };
        var (service, sequenceRepository, postRepository) = RegenerationService(field, posts);

        var result = await service.RegenerateAsync(
            new RegenerateMetaValuesQuery { PostTypeName = "post", Mode = MetaValuesRegenerationMode.All, StatusSlugs = ["published"] },
            CancellationToken.None);

        result.Should().Be(new RegenerateMetaValuesResult(1, 1));
        await sequenceRepository.Received().SetValueAsync(field.Id, "ВУ", 1, Arg.Any<CancellationToken>());
    }
}
