using System.Text.Json.Nodes;
using FluentAssertions;
using Mars.Cms.Contracts.PostTypes;
using Mars.Forms.Contracts;

namespace Mars.Server.Tests.Dto;

/// <summary>
/// Раскладка формы редактирования поста в общих опциях типа (<c>post_types.Options["form"]</c>):
/// ключ соседствует с другими опциями, null-раскладка убирает ключ.
/// </summary>
public class PostTypeOptionsCatalogTests
{
    [Fact]
    public void WithFormLayout_KeepsOtherOptionKeys_AndDoesNotTouchSource()
    {
        var options = new JsonObject { ["other"] = 1 };

        var updated = options.WithFormLayout(Layout());

        updated!.AsObject()["other"]!.GetValue<int>().Should().Be(1);
        updated.AsObject().ContainsKey(PostTypeOptionsCatalog.Form).Should().BeTrue();
        options.ContainsKey(PostTypeOptionsCatalog.Form).Should().BeFalse("мешок опций копируется, а не меняется на месте");
    }

    [Fact]
    public void WithFormLayout_NullLayout_RemovesKey()
    {
        var options = new JsonObject { ["other"] = 1, [PostTypeOptionsCatalog.Form] = Layout().ToJsonNode()! };

        var updated = options.WithFormLayout(null)!.AsObject();

        updated.ContainsKey(PostTypeOptionsCatalog.Form).Should().BeFalse();
        updated["other"]!.GetValue<int>().Should().Be(1);
    }

    [Fact]
    public void WithFormLayout_EmptyOptionsBag_BecomesNull()
    {
        ((JsonNode?)null).WithFormLayout(null).Should().BeNull();
        new JsonObject { [PostTypeOptionsCatalog.Form] = Layout().ToJsonNode()! }
            .WithFormLayout(null).Should().BeNull("опции без ключей не хранятся");
    }

    [Fact]
    public void GetFormLayout_MissingOrBrokenJson_IsNull()
    {
        ((JsonNode?)null).GetFormLayout().Should().BeNull();
        new JsonObject().GetFormLayout().Should().BeNull();
        new JsonObject { [PostTypeOptionsCatalog.Form] = "не json" }.GetFormLayout().Should().BeNull();
    }

    [Fact]
    public void FormLayout_RoundTripsZonesAndSections()
    {
        var options = ((JsonNode?)null).WithFormLayout(Layout());

        var parsed = options.GetFormLayout();

        parsed.Should().NotBeNull();
        parsed!.Items.Should().HaveCount(2);

        var tags = parsed.Items.First();
        tags.Key.Should().Be(SystemFieldsCatalog.Tags);
        tags.Zone.Should().Be(SystemFieldsCatalog.Zones.Main);
        tags.Visible.Should().BeFalse();
        tags.Width.Should().Be(FormItemWidths.Half);
        tags.Field.Should().BeNull("дескрипторы не хранятся — их отдаёт провайдер");

        var section = parsed.Items.Last();
        section.IsSection.Should().BeTrue();
        section.Title.Should().Be("Дополнительно");
        section.Collapsed.Should().BeTrue();
        section.Items.Single().Key.Should().Be(SystemFieldsCatalog.Slug);
    }

    //=====================================
    // параметры системных полей

    [Fact]
    public void WithSystemFields_KeepsOtherOptionKeys_AndDoesNotTouchSource()
    {
        var options = new JsonObject { ["other"] = 1, [PostTypeOptionsCatalog.Form] = Layout().ToJsonNode()! };

        var updated = options.WithSystemFields([Settings(SystemFieldsCatalog.Slug, FormRuleCatalog.Unique)]);

        updated!.AsObject()["other"]!.GetValue<int>().Should().Be(1);
        updated.AsObject().ContainsKey(PostTypeOptionsCatalog.Form).Should().BeTrue("ключи опций независимы");
        updated.AsObject().ContainsKey(PostTypeOptionsCatalog.SystemFields).Should().BeTrue();
        options.ContainsKey(PostTypeOptionsCatalog.SystemFields).Should().BeFalse("мешок опций копируется, а не меняется на месте");
    }

    [Fact]
    public void WithSystemFields_NullOrEmpty_RemovesKey()
    {
        var options = new JsonObject
        {
            ["other"] = 1,
            [PostTypeOptionsCatalog.SystemFields] = new JsonArray(),
        };

        options.WithSystemFields(null)!.AsObject().ContainsKey(PostTypeOptionsCatalog.SystemFields).Should().BeFalse();
        options.WithSystemFields([])!.AsObject().ContainsKey(PostTypeOptionsCatalog.SystemFields)
               .Should().BeFalse("пустой набор параметров не хранится");
        options["other"]!.GetValue<int>().Should().Be(1);
    }

    [Fact]
    public void GetSystemFields_MissingOrBrokenJson_IsNull()
    {
        ((JsonNode?)null).GetSystemFields().Should().BeNull();
        new JsonObject().GetSystemFields().Should().BeNull();
        new JsonObject { [PostTypeOptionsCatalog.SystemFields] = "не json" }.GetSystemFields().Should().BeNull();
    }

    [Fact]
    public void SystemFields_RoundTripsEditorAndRules()
    {
        var options = ((JsonNode?)null).WithSystemFields(
        [
            Settings(SystemFieldsCatalog.Slug, FormRuleCatalog.Unique, editor: FormEditorCatalog.Multiline),
            Settings(SystemFieldsCatalog.Excerpt),
        ]);

        var parsed = options.GetSystemFields();

        parsed.Should().NotBeNull();
        parsed!.Should().HaveCount(2);

        var slug = parsed.First();
        slug.Key.Should().Be(SystemFieldsCatalog.Slug);
        slug.Editor.Should().Be(FormEditorCatalog.Multiline);
        slug.Rules.Single().Type.Should().Be(FormRuleCatalog.Unique);

        parsed.Last().Key.Should().Be(SystemFieldsCatalog.Excerpt);
        parsed.Last().Rules.Should().BeEmpty();
    }

    //=====================================

    static FormFieldSettings Settings(string key, string? rule = null, string? editor = null) => new()
    {
        Key = key,
        Editor = editor,
        Rules = rule is null ? [] : [new FormRuleDefinition { Type = rule }],
    };

    static FormLayoutSettings Layout() => new()
    {
        Items =
        [
            new FormItem
            {
                Key = SystemFieldsCatalog.Tags,
                Zone = SystemFieldsCatalog.Zones.Main,
                Visible = false,
                Width = FormItemWidths.Half,
            },
            new FormItem
            {
                Key = "extra-section",
                Kind = FormItemKinds.Section,
                Zone = SystemFieldsCatalog.Zones.Extra,
                Title = "Дополнительно",
                Collapsed = true,
                Items = [new FormItem { Key = SystemFieldsCatalog.Slug }],
            },
        ],
    };
}
