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
        var postType = Type(AllFeatures, Meta("subtitle", 1), Meta("note", 2));

        var form = PostFormBuilder.Build(postType, Normalizer);

        form.Fields().Select(i => (i.Key, i.Zone)).Should().Equal(
            (SystemFieldsCatalog.Title, "main"),
            (SystemFieldsCatalog.Slug, "main"),
            (SystemFieldsCatalog.Content, "main"),
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
        form.Items.Where(i => i.Field is not null).Should()
            .OnlyContain(i => i.Parent!.StartsWith("column-"), "элементы живут только в колонках");
    }

    [Fact]
    public void FeatureGates_RemoveSlots()
    {
        var postType = Type([PostTypeConstants.Features.Content], Meta("subtitle", 1));

        var keys = PostFormBuilder.Build(postType, Normalizer).Fields().Select(i => i.Key);

        keys.Should().Equal(SystemFieldsCatalog.Title, SystemFieldsCatalog.Slug,
            SystemFieldsCatalog.Content, "subtitle",
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
    public void ContentSlot_IsSystemSlot_WithHeavyEditorFromTypeSettings()
    {
        var postType = Type(AllFeatures);

        var content = PostFormBuilder.Build(postType, Normalizer).Items.Single(i => i.Key == SystemFieldsCatalog.Content);

        content.Zone.Should().Be(SystemFieldsCatalog.Zones.Main);
        content.Field!.Type.Should().Be(FormFieldType.Text);
        content.Field.TitleKey.Should().Be("Content");
        content.Field.Editor.Should().Be(FormEditorCatalog.BlockEditor, "по умолчанию контент правит блочный редактор");
        content.Field.Rules.Should().BeEmpty();
    }

    [Fact]
    public void ContentSlot_TakesEditorAndCodeLangFromTypeSettings()
    {
        var postType = Type(AllFeatures) with
        {
            SystemFields =
            [
                new FormFieldSettings
                {
                    Key = SystemFieldsCatalog.Content,
                    Editor = FormEditorCatalog.Code,
                    CodeLang = "scriban",
                },
            ],
        };

        var content = PostFormBuilder.Build(postType, Normalizer).Items.Single(i => i.Key == SystemFieldsCatalog.Content);

        content.Field!.Editor.Should().Be(FormEditorCatalog.Code);
        content.Field.Options.GetCodeLang().Should().Be("scriban", "язык кода едет в дескрипторе — редактору он нужен без настроек типа");
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
                },
                new FormItem
                {
                    Key = "subtitle",
                    Zone = SystemFieldsCatalog.Zones.Main,
                    Visible = false,
                },
            ],
        };
        var postType = Type(AllFeatures, Meta("subtitle", 1)) with { Form = layout };

        var form = PostFormBuilder.Build(postType, Normalizer);

        form.Fields().Select(i => i.Key).Should().StartWith(SystemFieldsCatalog.Tags, "subtitle");

        var tags = form.Fields().First(i => i.Key == SystemFieldsCatalog.Tags);
        tags.Zone.Should().Be(SystemFieldsCatalog.Zones.Main);
        tags.Field!.Rules.Should().BeEmpty();
        tags.Field.Editor.Should().Be(FormEditorCatalog.Tags, "редактор слота — из общего каталога и параметров типа");

        form.Fields().First(i => i.Key == "subtitle").Visible.Should().BeFalse();
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
