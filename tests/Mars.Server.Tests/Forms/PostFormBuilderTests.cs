using System.Text.Json.Nodes;
using FluentAssertions;
using Mars.Cms.Abstractions.Forms;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Contracts.PostTypes;
using Mars.Forms.Contracts;
using static Mars.Server.Tests.Forms.PostFormTestHost;

namespace Mars.Server.Tests.Forms;

public class PostFormBuilderTests
{
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
    public void SavedLayout_ReordersAndKeepsZones_ButCarriesNoFieldSettings()
    {
        var layout = new FormLayoutSettings
        {
            Items =
            [
                new FormItem
                {
                    Key = SystemFieldsCatalog.Tags,
                    Zone = SystemFieldsCatalog.Zones.Main,
                    // легаси-хранилище: правила и редактор в раскладке больше не действуют
                    Rules = [new FormRuleDefinition { Type = FormRuleCatalog.Length, Params = new JsonObject { ["min"] = 2 } }],
                    Editor = FormEditorCatalog.Text,
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
        tags.Rules.Should().BeEmpty("раскладка отвечает только за представление");
        tags.Field!.Rules.Should().BeEmpty();
        tags.Field.Editor.Should().Be(PostFormEditors.Tags, "редактор слота — из каталога и параметров типа, не из раскладки");

        var subtitle = form.Items.First(i => i.Key == "subtitle");
        subtitle.Visible.Should().BeFalse();
        subtitle.Rules.Should().BeEmpty();
    }

    [Fact]
    public void SystemFieldSettings_OverrideEditorAndCarryRules()
    {
        var postType = Type(AllFeatures) with
        {
            SystemFields =
            [
                new FormFieldSettings
                {
                    Key = SystemFieldsCatalog.Excerpt,
                    Editor = FormEditorCatalog.Multiline,
                    Rules = [new FormRuleDefinition { Type = FormRuleCatalog.Length, Params = new JsonObject { ["max"] = 200 } }],
                },
            ],
        };

        var excerpt = PostFormBuilder.Build(postType, Normalizer)
                                     .Items.Single(i => i.Key == SystemFieldsCatalog.Excerpt);

        excerpt.Field!.Editor.Should().Be(FormEditorCatalog.Multiline);
        excerpt.Field.Rules.Single().Type.Should().Be(FormRuleCatalog.Length);
        excerpt.Rules.Should().BeEmpty("правила приходят в дескрипторе, а не в элементе раскладки");
    }

    [Fact]
    public void SystemFieldSettings_OtherSlotsKeepCatalogDefaults()
    {
        var postType = Type(AllFeatures) with
        {
            SystemFields = [new FormFieldSettings { Key = SystemFieldsCatalog.Excerpt, Editor = FormEditorCatalog.Multiline }],
        };

        var form = PostFormBuilder.Build(postType, Normalizer);

        form.Items.Single(i => i.Key == SystemFieldsCatalog.Title).Field!.Editor.Should().Be(PostFormEditors.Title);
        form.Items.Single(i => i.Key == SystemFieldsCatalog.Status).Field!.Rules.Should().BeEmpty();
    }

    [Fact]
    public void SystemFieldSettings_UnknownKeyIsIgnored()
    {
        var postType = Type(AllFeatures, Meta("subtitle", 1)) with
        {
            SystemFields = [new FormFieldSettings { Key = "subtitle", Rules = [new FormRuleDefinition { Type = FormRuleCatalog.Required }] }],
        };

        var subtitle = PostFormBuilder.Build(postType, Normalizer).Fields().Single(i => i.Key == "subtitle");

        subtitle.Field!.Rules.Should().BeEmpty("правила метаполей живут на определении поля");
    }

    [Fact]
    public void Zones_AreDeclaredInOrder()
    {
        var form = PostFormBuilder.Build(Type(AllFeatures), Normalizer);

        form.OwnerModel.Should().Be("post.article");
        form.Zones.Select(z => z.Key).Should().Equal("main", "publish", "extra");
    }

    [Fact]
    public void OwnerModel_RoundTripsTypeName()
    {
        PostFormBuilder.OwnerModel("article").Should().Be("post.article");
        PostFormBuilder.PostTypeName("post.article").Should().Be("article");
        PostFormBuilder.OwnerModelWildcard.Should().Be("post.*");
    }
}
