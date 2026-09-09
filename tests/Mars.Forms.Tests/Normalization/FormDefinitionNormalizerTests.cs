using FluentAssertions;
using Mars.Forms.Abstractions.Services;
using Mars.Forms.Contracts;

namespace Mars.Forms.Tests.Normalization;

public class FormDefinitionNormalizerTests
{
    readonly FormDefinitionNormalizer _normalizer = new();

    [Fact]
    public void NoSavedLayout_ReturnsDefaultsInOrder()
    {
        var result = _normalizer.Normalize(null, Defaults());

        result.Select(i => i.Key).Should().Equal("title", "slug", "status");
        result.Select(i => i.Zone).Should().Equal("main", "main", "side");
        result.Should().OnlyContain(i => i.Field != null);
    }

    [Fact]
    public void SavedOrder_Wins_DefaultsFillTheTail()
    {
        var saved = new List<FormItem>
        {
            new() { Key = "status", Zone = "side" },
            new() { Key = "slug", Zone = "main" },
        };

        var result = _normalizer.Normalize(saved, Defaults());

        // зоны рендерятся раздельно, поэтому сохранённый порядок держится внутри зоны
        result.Select(i => (i.Key, i.Zone)).Should().Equal(("slug", "main"), ("title", "main"), ("status", "side"));
    }

    [Fact]
    public void UnknownAndDuplicateKeys_AreDropped()
    {
        var saved = new List<FormItem>
        {
            new() { Key = "ghost", Zone = "main" },
            new() { Key = "title", Zone = "main" },
            new() { Key = "title", Zone = "main" },
        };

        var result = _normalizer.Normalize(saved, Defaults());

        result.Select(i => i.Key).Should().Equal("title", "slug", "status");
    }

    [Fact]
    public void FieldMovedToAnotherZone_StaysThere_AndIsNotDuplicated()
    {
        var saved = new List<FormItem> { new() { Key = "status", Zone = "main" } };

        var result = _normalizer.Normalize(saved, Defaults());

        result.Select(i => (i.Key, i.Zone)).Should().Equal(("status", "main"), ("title", "main"), ("slug", "main"));
    }

    [Fact]
    public void UnknownZone_KeepsItem_AndGoesAfterKnownZones()
    {
        var saved = new List<FormItem> { new() { Key = "title", Zone = "extra" } };

        var result = _normalizer.Normalize(saved, Defaults());

        result.Select(i => (i.Key, i.Zone)).Should().Equal(("slug", "main"), ("status", "side"), ("title", "extra"));
    }

    [Fact]
    public void Section_IsPreserved_WithFilteredChildren()
    {
        var saved = new List<FormItem>
        {
            new()
            {
                Key = "group-1",
                Kind = FormItemKinds.Section,
                Zone = "main",
                Title = "Основное",
                Collapsed = true,
                Items = [new FormItem { Key = "title" }, new FormItem { Key = "ghost" }],
            },
        };

        var result = _normalizer.Normalize(saved, Defaults());

        var section = result.First();
        section.IsSection.Should().BeTrue();
        section.Title.Should().Be("Основное");
        section.Collapsed.Should().BeTrue();
        section.Items.Select(i => i.Key).Should().Equal("title");
        section.Field.Should().BeNull();
        result.Select(i => i.Key).Should().Equal("group-1", "slug", "status");
    }

    [Fact]
    public void NestedSection_OverDepthLimit_HoistsChildren()
    {
        var saved = new List<FormItem>
        {
            new()
            {
                Key = "outer",
                Kind = FormItemKinds.Section,
                Zone = "main",
                Items =
                [
                    new FormItem
                    {
                        Key = "inner",
                        Kind = FormItemKinds.Section,
                        Items = [new FormItem { Key = "title" }],
                    },
                ],
            },
        };

        var result = _normalizer.Normalize(saved, Defaults());

        var outer = result.First();
        outer.Items.Select(i => i.Key).Should().Equal("title");
        outer.Items.Should().OnlyContain(i => !i.IsSection);
    }

    [Fact]
    public void SectionWithoutKey_IsFlattened()
    {
        var saved = new List<FormItem>
        {
            new() { Key = "", Kind = FormItemKinds.Section, Zone = "main", Items = [new FormItem { Key = "title" }] },
        };

        var result = _normalizer.Normalize(saved, Defaults());

        result.Select(i => i.Key).Should().Equal("title", "slug", "status");
    }

    [Fact]
    public void DuplicateSection_KeptOnce()
    {
        var saved = new List<FormItem>
        {
            new() { Key = "g", Kind = FormItemKinds.Section, Zone = "main", Items = [new FormItem { Key = "title" }] },
            new() { Key = "g", Kind = FormItemKinds.Section, Zone = "main", Items = [new FormItem { Key = "slug" }] },
        };

        var result = _normalizer.Normalize(saved, Defaults());

        result.Count(i => i.IsSection).Should().Be(1);
        result.Select(i => i.Key).Should().Equal("g", "slug", "status");
    }

    [Fact]
    public void RulesAndEditor_SurviveOnlyForFormOwnedSettings()
    {
        var saved = new List<FormItem>
        {
            new()
            {
                Key = "title",
                Zone = "main",
                Editor = "core.input.url",
                Rules = [new FormRuleDefinition { Type = FormRuleCatalog.Regex }],
            },
            new()
            {
                Key = "slug",
                Zone = "main",
                Editor = "core.input.url",
                Rules = [new FormRuleDefinition { Type = FormRuleCatalog.Length }],
            },
        };

        var result = _normalizer.Normalize(saved, Defaults());

        // title — метаполе (правила на определении поля), раскладка их не przechowывает
        result.First(i => i.Key == "title").Rules.Should().BeEmpty();
        result.First(i => i.Key == "title").Editor.Should().BeNull();

        // slug — системное поле: правила и редактор живут в раскладке
        result.First(i => i.Key == "slug").Rules.Select(r => r.Type).Should().Equal(FormRuleCatalog.Length);
        result.First(i => i.Key == "slug").Editor.Should().Be("core.input.url");
    }

    [Fact]
    public void Descriptor_IsAlwaysTakenFromDefaults_NotFromSavedLayout()
    {
        var saved = new List<FormItem>
        {
            new()
            {
                Key = "title",
                Zone = "main",
                Field = new FormFieldDescriptor { Key = "title", Title = "Устаревший", Type = FormFieldType.Text },
            },
        };

        var result = _normalizer.Normalize(saved, Defaults());

        result.First().Field!.Title.Should().Be("title");
        result.First().Field!.Type.Should().Be(FormFieldType.String);
    }

    [Fact]
    public void Normalize_IsIdempotent()
    {
        var saved = new List<FormItem>
        {
            new()
            {
                Key = "group-1",
                Kind = FormItemKinds.Section,
                Zone = "main",
                Title = "Основное",
                Items = [new FormItem { Key = "slug", Width = FormItemWidths.Half, Visible = false }],
            },
            new() { Key = "ghost", Zone = "side" },
        };

        var once = _normalizer.Normalize(saved, Defaults());
        var twice = _normalizer.Normalize(once, Defaults());

        twice.Should().BeEquivalentTo(once);
    }

    [Fact]
    public void LayoutSettings_KeepVisibleAndWidth()
    {
        var saved = new List<FormItem> { new() { Key = "title", Zone = "main", Visible = false, Width = FormItemWidths.Third } };

        var item = _normalizer.Normalize(saved, Defaults()).First();

        item.Visible.Should().BeFalse();
        item.Width.Should().Be(FormItemWidths.Third);
    }

    [Fact]
    public void SectionWithoutZone_GetsFirstZone()
    {
        var saved = new List<FormItem>
        {
            new() { Key = "group-1", Kind = FormItemKinds.Section, Items = [new FormItem { Key = "slug" }] },
        };

        var section = _normalizer.Normalize(saved, Defaults()).First();

        section.IsSection.Should().BeTrue();
        section.Zone.Should().Be("main", "иначе рендерер зоны секцию не увидит");
        section.Items.Single().Zone.Should().BeNull("зона хранится только у корневых элементов");
    }

    static IReadOnlyCollection<FormItem> Defaults() =>
    [
        Field("title", "main"),
        Field("slug", "main", settingsOnForm: true),
        Field("status", "side", settingsOnForm: true),
    ];

    static FormItem Field(string key, string zone, bool settingsOnForm = false) => new()
    {
        Key = key,
        Zone = zone,
        Field = new FormFieldDescriptor
        {
            Key = key,
            Title = key,
            Type = FormFieldType.String,
            SettingsOnForm = settingsOnForm,
        },
    };
}
