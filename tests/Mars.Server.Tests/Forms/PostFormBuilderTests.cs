using System.Text.Json.Nodes;
using FluentAssertions;
using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Dto.Posts;
using Mars.Cms.Abstractions.Dto.PostTypes;
using Mars.Cms.Abstractions.Forms;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Contracts.PostTypes;
using Mars.Forms.Abstractions;
using Mars.Forms.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Server.Tests.Forms;

public class PostFormBuilderTests
{
    static readonly IFormDefinitionNormalizer Normalizer =
        new ServiceCollection().AddMarsForms().BuildServiceProvider().GetRequiredService<IFormDefinitionNormalizer>();

    static readonly string[] AllFeatures =
    [
        PostTypeConstants.Features.Content,
        PostTypeConstants.Features.Status,
        PostTypeConstants.Features.ModifyCreatedDate,
        PostTypeConstants.Features.Language,
        PostTypeConstants.Features.Tags,
        PostTypeConstants.Features.Excerpt,
        PostTypeConstants.Features.Category,
    ];

    [Fact]
    public void DefaultTree_KeepsHistoricOrderAndZones()
    {
        var postType = Type(AllFeatures, Content(), Meta("subtitle", 1), Meta("note", 2));

        var form = PostFormBuilder.Build(postType, Normalizer);

        form.Items.Select(i => (i.Key, i.Zone)).Should().Equal(
            (SystemFieldsCatalog.Title, "main"),
            (SystemFieldsCatalog.Slug, "main"),
            (FeatureFieldsCatalog.ContentFieldKey, "main"),
            (SystemFieldsCatalog.Excerpt, "main"),
            ("subtitle", "main"),
            ("note", "main"),
            (SystemFieldsCatalog.CreatedAt, "publish"),
            (SystemFieldsCatalog.ModifiedAt, "publish"),
            (SystemFieldsCatalog.Status, "publish"),
            (SystemFieldsCatalog.Lang, "publish"),
            (SystemFieldsCatalog.Author, "publish"),
            (SystemFieldsCatalog.Categories, "extra"),
            (SystemFieldsCatalog.Tags, "extra"));
        form.OwnerModel.Should().Be("post.article");
    }

    [Fact]
    public void FeatureGates_RemoveSlots()
    {
        var postType = Type([PostTypeConstants.Features.Content], Content(), Meta("subtitle", 1));

        var keys = PostFormBuilder.Build(postType, Normalizer).Items.Select(i => i.Key);

        keys.Should().Equal(SystemFieldsCatalog.Title, SystemFieldsCatalog.Slug,
            FeatureFieldsCatalog.ContentFieldKey, "subtitle",
            SystemFieldsCatalog.CreatedAt, SystemFieldsCatalog.ModifiedAt, SystemFieldsCatalog.Author);
    }

    [Fact]
    public void MetaFields_AreOrdered_AndDisabledQueryHiddenExcluded()
    {
        var postType = Type(AllFeatures,
            Meta("b", 2),
            Meta("a", 1),
            Meta("off", 3, disabled: true),
            Meta("calc", 4, type: MetaFieldType.Query),
            Meta("secret", 5, hidden: true));

        var serverKeys = PostFormBuilder.Build(postType, Normalizer).Fields().Select(i => i.Key).ToArray();
        var clientKeys = PostFormBuilder.Build(postType, Normalizer, client: true).Fields().Select(i => i.Key).ToArray();

        serverKeys.Should().ContainInOrder("a", "b", "secret");
        serverKeys.Should().NotContain("off").And.NotContain("calc");
        clientKeys.Should().ContainInOrder("a", "b");
        clientKeys.Should().NotContain("secret");
    }

    [Fact]
    public void ContentSlot_TakenFromMetaField_WithOptionsAndNotDuplicated()
    {
        var postType = Type(AllFeatures, Meta(FeatureFieldsCatalog.ContentFieldKey, 1,
            type: MetaFieldType.Text, featureKey: FeatureFieldsCatalog.Content));

        var form = PostFormBuilder.Build(postType, Normalizer);

        var content = form.Items.Single(i => i.Key == FeatureFieldsCatalog.ContentFieldKey);
        content.Field!.Type.Should().Be(FormFieldType.Text);
        content.Field.Options.GetFeatureKey().Should().Be(FeatureFieldsCatalog.Content);
        content.Field.SettingsOnForm.Should().BeFalse("правила метаполя живут на определении поля");
        form.Items.Count(i => i.Key == FeatureFieldsCatalog.ContentFieldKey).Should().Be(1);
    }

    [Fact]
    public void CreatedAt_EditableOnlyWithModifyCreatedDateFeature()
    {
        var editable = PostFormBuilder.Build(Type([PostTypeConstants.Features.ModifyCreatedDate]), Normalizer)
            .Items.Single(i => i.Key == SystemFieldsCatalog.CreatedAt);
        var readOnly = PostFormBuilder.Build(Type([]), Normalizer)
            .Items.Single(i => i.Key == SystemFieldsCatalog.CreatedAt);

        editable.Field!.ReadOnly.Should().BeFalse();
        readOnly.Field!.ReadOnly.Should().BeTrue();
    }

    [Fact]
    public void SystemSlots_CarryResourceTitleKey_AndFormOwnedSettings()
    {
        var title = PostFormBuilder.Build(Type(AllFeatures), Normalizer)
            .Items.Single(i => i.Key == SystemFieldsCatalog.Title);

        title.Field!.TitleKey.Should().Be("Title");
        title.Field.SettingsOnForm.Should().BeTrue();
        title.Field.Required.Should().BeTrue();
        title.Field.Editor.Should().Be(PostFormEditors.Title);
    }

    [Fact]
    public void StatusChoices_AreTypeStatuses_WithSlugAsKey()
    {
        var postType = Type([PostTypeConstants.Features.Status],
            statuses: [Status("draft", "Черновик"), Status("published", "Опубликован")]);

        var status = PostFormBuilder.Build(postType, Normalizer).Items.Single(i => i.Key == SystemFieldsCatalog.Status);

        status.Field!.Choices.Select(c => (c.Key, c.Title)).Should()
              .Equal(("draft", "Черновик"), ("published", "Опубликован"));
    }

    [Fact]
    public void SavedLayout_ReordersKeepsZones_AndCarriesRulesOfSystemSlotsOnly()
    {
        var layout = new FormLayoutSettings
        {
            Items =
            [
                new FormItem
                {
                    Key = SystemFieldsCatalog.Tags,
                    Zone = SystemFieldsCatalog.Zones.Main,
                    Rules = [new FormRuleDefinition { Type = FormRuleCatalog.Length, Params = new JsonObject { ["min"] = 2 } }],
                },
                new FormItem
                {
                    Key = "subtitle",
                    Zone = SystemFieldsCatalog.Zones.Main,
                    Visible = false,
                    Rules = [new FormRuleDefinition { Type = FormRuleCatalog.Regex }],
                },
            ],
        };
        var postType = Type(AllFeatures, Meta("subtitle", 1)) with { Form = layout };

        var form = PostFormBuilder.Build(postType, Normalizer);

        form.Items.Select(i => i.Key).Should().StartWith(SystemFieldsCatalog.Tags, "subtitle");
        var tags = form.Items.First(i => i.Key == SystemFieldsCatalog.Tags);
        tags.Zone.Should().Be(SystemFieldsCatalog.Zones.Main);
        tags.Rules.Single().Type.Should().Be(FormRuleCatalog.Length);

        var subtitle = form.Items.First(i => i.Key == "subtitle");
        subtitle.Visible.Should().BeFalse();
        subtitle.Rules.Should().BeEmpty();
    }

    [Fact]
    public void Manifest_DescribesZonesAndCapabilities()
    {
        var manifest = PostFormBuilder.Build(Type(AllFeatures), Normalizer).Manifest;

        manifest!.OwnerModel.Should().Be("post.article");
        manifest.Zones.Select(z => z.Key).Should().Equal("main", "publish", "extra");
        manifest.ContainerKinds.Should().BeEquivalentTo(FormItemKinds.All);
        manifest.RuleTypes.Should().Contain(FormRuleCatalog.Unique);
        manifest.Capabilities.CanAddFields.Should().BeFalse();
        manifest.Capabilities.CanSubmit.Should().BeFalse("пост сохраняется типизированным API");
    }

    [Fact]
    public void OwnerModel_RoundTripsTypeName()
    {
        PostFormBuilder.OwnerModel("article").Should().Be("post.article");
        PostFormBuilder.PostTypeName("post.article").Should().Be("article");
        PostFormBuilder.OwnerModelWildcard.Should().Be("post.*");
    }

    //=====================================

    static PostTypeDetail Type(IReadOnlyCollection<string> features,
                               params MetaFieldDto[] metaFields)
        => Type(features, [], metaFields);

    static PostTypeDetail Type(IReadOnlyCollection<string> features,
                               IReadOnlyCollection<PostStatusDto> statuses,
                               IReadOnlyCollection<MetaFieldDto>? metaFields = null) => new()
    {
        Id = Guid.NewGuid(),
        CreatedAt = DateTimeOffset.Now,
        ModifiedAt = null,
        Title = "Статья",
        TypeName = "article",
        Tags = [],
        EnabledFeatures = features,
        Disabled = false,
        Visibility = PostTypeVisibility.Public,
        PostStatusList = statuses,
        MetaFields = metaFields ?? [],
        Presentation = PostTypePresentation.Default(),
    };

    static PostStatusDto Status(string slug, string title) => new()
    {
        Id = Guid.NewGuid(),
        Slug = slug,
        Title = title,
        Color = "",
        Order = 0,
    };

    /// <summary>Поле контента, которое создаёт фича Content</summary>
    static MetaFieldDto Content() => Meta(FeatureFieldsCatalog.ContentFieldKey, 0,
        type: MetaFieldType.Text, featureKey: FeatureFieldsCatalog.Content);

    static MetaFieldDto Meta(string key, int order, MetaFieldType type = MetaFieldType.String,
                             bool hidden = false, bool disabled = false, string? featureKey = null) => new()
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
        Options = featureKey is null ? null : new JsonObject { [FeatureFieldsCatalog.FeatureKeyOption()] = featureKey },
        Order = order,
        Tags = [],
        Hidden = hidden,
        Disabled = disabled,
        Variants = null,
        ModelName = null,
    };
}
