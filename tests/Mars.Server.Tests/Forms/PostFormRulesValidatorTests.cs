using System.Text.Json.Nodes;
using FluentAssertions;
using Mars.Cms.Abstractions.Dto.Posts;
using Mars.Cms.Abstractions.Forms;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Contracts.PostTypes;
using Mars.Forms.Contracts;
using NSubstitute;
using static Mars.Server.Tests.Forms.PostFormTestHost;

namespace Mars.Server.Tests.Forms;

/// <summary>
/// Правила системных полей поста (параметры типа, <c>post_types.Options["systemFields"]</c>) на сервере:
/// применяются ко всем путям записи, не подменяют пол транспорта (DataAnnotations и правила
/// <see cref="GeneralPostQueryValidator"/>).
/// </summary>
public class PostFormRulesValidatorTests
{
    [Fact]
    public async Task SystemFieldRules_ApplyToSystemSlots()
    {
        var postType = Type(AllFeatures) with
        {
            SystemFields =
            [
                SlotSettings(SystemFieldsCatalog.Title, [Rule(FormRuleCatalog.Regex, new JsonObject { ["pattern"] = @"^\d+$" })]),
                SlotSettings(SystemFieldsCatalog.Slug, [Rule(FormRuleCatalog.Length, new JsonObject { ["min"] = 10 })]),
            ],
        };
        var validator = RulesValidator(postType, out _);

        var errors = await validator.ValidateAsync(CreateQuery(title: "abc", slug: "short"), id: null);

        errors.Select(e => e.Key).Should()
              .BeEquivalentTo([SystemFieldsCatalog.Title, SystemFieldsCatalog.Slug]);
    }

    [Fact]
    public async Task ConformingValues_Pass()
    {
        var postType = Type(AllFeatures) with
        {
            SystemFields =
            [
                SlotSettings(SystemFieldsCatalog.Title, [Rule(FormRuleCatalog.Length, new JsonObject { ["min"] = 3 })]),
                SlotSettings(SystemFieldsCatalog.Slug, [Rule(FormRuleCatalog.Regex, new JsonObject { ["pattern"] = "^[a-z-]+$" })]),
            ],
        };
        var validator = RulesValidator(postType, out _);

        var errors = await validator.ValidateAsync(CreateQuery(), id: null);

        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task WithoutFieldRules_DescriptorFloorIsNotApplied()
    {
        // title и slug обязательны в дескрипторе слота, но их пол — транспорт: пустой заголовок
        // без правила в параметрах типа ошибку формы не даёт
        var validator = RulesValidator(Type(AllFeatures), out _);

        var errors = await validator.ValidateAsync(CreateQuery(title: "", slug: ""), id: null);

        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task RequiredRule_FromSettings_FiresOnEmptyValue()
    {
        var postType = Type(AllFeatures) with
        {
            SystemFields = [SlotSettings(SystemFieldsCatalog.Excerpt, [Rule(FormRuleCatalog.Required)])],
        };
        var validator = RulesValidator(postType, out _);

        var errors = await validator.ValidateAsync(CreateQuery(excerpt: null), id: null);

        errors.Single().Key.Should().Be(SystemFieldsCatalog.Excerpt);
    }

    [Fact]
    public void RulesOnly_KeepsTransportSlotsWithRules()
    {
        var postType = Type(AllFeatures, Meta("subtitle", 1)) with
        {
            SystemFields =
            [
                SlotSettings(SystemFieldsCatalog.CreatedAt, [Rule(FormRuleCatalog.Required)]),
                // ключ метаполя в параметрах системных полей игнорируется: правила метаполей живёт в их пайплайне
                SlotSettings("subtitle", [Rule(FormRuleCatalog.Required)]),
                SlotSettings(SystemFieldsCatalog.Tags, [Rule(FormRuleCatalog.Length)]),
            ],
        };

        var keys = PostFormRulesValidator.RulesOnly(PostFormBuilder.Build(postType, Normalizer))
                                         .Fields().Select(i => i.Key);

        // даты транспорт записи не несёт
        keys.Should().Equal(SystemFieldsCatalog.Tags);
    }

    [Fact]
    public async Task UniqueSlug_OnCreate_FailsWhenOccupied()
    {
        var postType = Type(AllFeatures) with
        {
            SystemFields = [SlotSettings(SystemFieldsCatalog.Slug, [Rule(FormRuleCatalog.Unique)])],
        };
        var validator = RulesValidator(postType, out var posts);
        posts.SlugOccupiedAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
             .Returns(true);

        var errors = await validator.ValidateAsync(CreateQuery(), id: null);

        errors.Single().Key.Should().Be(SystemFieldsCatalog.Slug);
        errors.Single().Message.Should().Be("значение уже занято");
        await posts.Received(1).SlugOccupiedAsync(Arg.Is("article"), Arg.Is("my-slug"),
            Arg.Is<Guid?>(x => x == null), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UniqueSlug_OnUpdate_ExcludesOwnPost()
    {
        var id = Guid.NewGuid();
        var postType = Type(AllFeatures) with
        {
            SystemFields = [SlotSettings(SystemFieldsCatalog.Slug, [Rule(FormRuleCatalog.Unique)])],
        };
        var validator = RulesValidator(postType, out var posts);
        posts.SlugOccupiedAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
             .Returns(true);

        var errors = await validator.ValidateAsync(UpdateQuery(id), id);

        errors.Single().Key.Should().Be(SystemFieldsCatalog.Slug);
        await posts.Received(1).SlugOccupiedAsync(Arg.Is("article"), Arg.Is("my-slug"),
            Arg.Is<Guid?>(x => x == id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UniqueSlug_OnUpdate_OwnSlugPasses()
    {
        var id = Guid.NewGuid();
        var postType = Type(AllFeatures) with
        {
            SystemFields = [SlotSettings(SystemFieldsCatalog.Slug, [Rule(FormRuleCatalog.Unique)])],
        };
        var validator = RulesValidator(postType, out var posts);
        posts.SlugOccupiedAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
             .Returns(false);

        var errors = await validator.ValidateAsync(UpdateQuery(id), id);

        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task UniqueRule_OnOtherSlot_IsIgnored()
    {
        var postType = Type(AllFeatures) with
        {
            SystemFields = [SlotSettings(SystemFieldsCatalog.Title, [Rule(FormRuleCatalog.Unique)])],
        };
        var validator = RulesValidator(postType, out var posts);

        var errors = await validator.ValidateAsync(CreateQuery(), id: null);

        errors.Should().BeEmpty();
        await posts.DidNotReceiveWithAnyArgs().SlugOccupiedAsync(default!, default!, default, default);
    }

    [Fact]
    public async Task RuleMessage_OverriddenByParams()
    {
        var postType = Type(AllFeatures) with
        {
            SystemFields =
            [
                SlotSettings(SystemFieldsCatalog.Slug,
                    [Rule(FormRuleCatalog.Unique, new JsonObject { ["message"] = "такой slug уже есть" })]),
            ],
        };
        var validator = RulesValidator(postType, out var posts);
        posts.SlugOccupiedAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
             .Returns(true);

        var errors = await validator.ValidateAsync(CreateQuery(), id: null);

        errors.Single().Message.Should().Be("такой slug уже есть");
    }

    [Fact]
    public async Task UnknownPostType_ProducesNoFormErrors()
    {
        var validator = RulesValidator(Type(AllFeatures), out _);

        var errors = await validator.ValidateAsync(CreateQuery() with { Type = "unknown" }, id: null);

        errors.Should().BeEmpty("тип и его доступность проверяет основное правило валидатора запроса");
    }

    [Fact]
    public void OwnerId_TakenFromQuery()
    {
        var id = Guid.NewGuid();

        PostFormRulesValidator.OwnerId(CreateQuery()).Should().BeNull();
        PostFormRulesValidator.OwnerId(CreateQuery() with { Id = id }).Should().Be(id);
        PostFormRulesValidator.OwnerId(UpdateQuery(id)).Should().Be(id);
    }

    [Fact]
    public void TransportProperty_MapsSlotsToQueryProperties()
    {
        PostFormRulesValidator.TransportProperty(SystemFieldsCatalog.Title).Should().Be(nameof(CreatePostQuery.Title));
        PostFormRulesValidator.TransportProperty(SystemFieldsCatalog.Slug).Should().Be(nameof(CreatePostQuery.Slug));
        PostFormRulesValidator.TransportProperty(SystemFieldsCatalog.Excerpt).Should().Be(nameof(CreatePostQuery.Excerpt));
        PostFormRulesValidator.TransportProperty(SystemFieldsCatalog.Status).Should().Be(nameof(CreatePostQuery.Status));
        PostFormRulesValidator.TransportProperty(SystemFieldsCatalog.Lang).Should().Be(nameof(CreatePostQuery.LangCode));
        PostFormRulesValidator.TransportProperty(SystemFieldsCatalog.Author).Should().Be(nameof(CreatePostQuery.UserId));
        PostFormRulesValidator.TransportProperty(SystemFieldsCatalog.Categories).Should().Be(nameof(CreatePostQuery.CategoryIds));
        PostFormRulesValidator.TransportProperty(SystemFieldsCatalog.Tags).Should().Be(nameof(CreatePostQuery.Tags));

        PostFormRulesValidator.TransportProperty(SystemFieldsCatalog.CreatedAt).Should().BeNull();
        PostFormRulesValidator.TransportProperty("subtitle").Should().BeNull();
    }

    [Fact]
    public async Task CreatePostQueryValidator_SurfacesFormRuleAsPropertyError()
    {
        var postType = Type(AllFeatures) with
        {
            SystemFields =
            [
                SlotSettings(SystemFieldsCatalog.Slug,
                    [Rule(FormRuleCatalog.Length, new JsonObject { ["min"] = 10 })]),
            ],
        };
        var locator = Substitute.For<IMetaModelTypesLocator>();
        locator.GetPostTypeByName(postType.TypeName).Returns(postType);
        var posts = Substitute.For<IPostRepository>();

        var validator = new CreatePostQueryValidator(locator,
            Substitute.For<IPostCategoryRepository>(),
            Substitute.For<IMetaValuesValidator>(),
            posts,
            RulesValidator(locator, posts));

        var result = await validator.ValidateAsync(CreateQuery(slug: "short"));

        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePostQuery.Slug)
                                            && e.ErrorMessage.Contains("минимальная длина 10"));
    }

    //=====================================

    static CreatePostQuery CreateQuery(string title = "Заголовок", string slug = "my-slug", string? excerpt = null)
        => new()
        {
            Title = title,
            Type = "article",
            Slug = slug,
            Tags = [],
            UserId = Guid.NewGuid(),
            Status = null,
            Content = null,
            Excerpt = excerpt,
            LangCode = "",
            CategoryIds = [],
            MetaValues = [],
        };

    static UpdatePostQuery UpdateQuery(Guid id, string title = "Заголовок", string slug = "my-slug")
        => new()
        {
            Id = id,
            Title = title,
            Type = "article",
            Slug = slug,
            Tags = [],
            UserId = Guid.NewGuid(),
            Status = null,
            Content = null,
            Excerpt = null,
            LangCode = "",
            CategoryIds = [],
            MetaValues = [],
        };
}
