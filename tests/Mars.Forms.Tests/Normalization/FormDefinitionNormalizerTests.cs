using FluentAssertions;
using Mars.Forms.Abstractions.Services;
using Mars.Forms.Contracts;

namespace Mars.Forms.Tests.Normalization;

/// <summary>
/// Раскладка плоская: сохранённый порядок и настройки выигрывают, неизвестные ключи
/// отбрасываются, недостающие поля провайдера дописываются в конец, дескрипторы всегда свежие.
/// Порядок сравнивается по зонам: зоны рендерятся раздельно.
/// </summary>
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

        result.Select(i => i.Key).Should().Equal("status", "slug", "title");
        KeysOf(result, "main").Should().Equal("slug", "title");
        KeysOf(result, "side").Should().Equal("status");
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
    public void SavedItemWithoutZone_FallsBackToDefaultZone()
    {
        var result = _normalizer.Normalize([new FormItem { Key = "status" }], Defaults());

        result.Single(i => i.Key == "status").Zone.Should().Be("side");
    }

    [Fact]
    public void FieldMovedToAnotherZone_StaysThere_AndIsNotDuplicated()
    {
        var saved = new List<FormItem> { new() { Key = "status", Zone = "main" } };

        var result = _normalizer.Normalize(saved, Defaults());

        result.Select(i => (i.Key, i.Zone)).Should().Equal(("status", "main"), ("title", "main"), ("slug", "main"));
    }

    [Fact]
    public void SectionMarker_IsKeptOnce_WithItsTitleAndNoField()
    {
        var saved = new List<FormItem>
        {
            new() { Key = "group-1", Zone = "main", Kind = FormItemKind.Heading, Title = "Основное" },
            new() { Key = "title", Zone = "main" },
            new() { Key = "group-1", Zone = "main", Kind = FormItemKind.Heading, Title = "Дубль" },
        };

        var result = _normalizer.Normalize(saved, Defaults());

        result.Select(i => i.Key).Should().Equal("group-1", "title", "slug", "status");

        var marker = result.First();
        marker.Kind.Should().Be(FormItemKind.Heading);
        marker.Title.Should().Be("Основное");
        marker.Field.Should().BeNull();
    }

    [Fact]
    public void SectionMarkerWithoutZone_GetsFirstZone()
    {
        var result = _normalizer.Normalize(
            [new FormItem { Key = "group-1", Kind = FormItemKind.Heading, Title = "Основное" }], Defaults());

        result.First().Zone.Should().Be("main");
    }

    [Fact]
    public void LayoutSettings_KeepVisibleWidthAndTitle()
    {
        var saved = new List<FormItem>
        {
            new() { Key = "title", Zone = "main", Visible = false, Width = FormItemWidths.Third, Title = "Заголовок" },
        };

        var item = _normalizer.Normalize(saved, Defaults()).First();

        item.Visible.Should().BeFalse();
        item.Width.Should().Be(FormItemWidths.Third);
        item.Title.Should().Be("Заголовок");
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
    public void DescriptorRulesAndEditor_SurviveNormalization()
    {
        var saved = new List<FormItem> { new() { Key = "slug", Zone = "main", Visible = false } };
        var defaults = new List<FormItem>
        {
            new()
            {
                Key = "slug",
                Zone = "main",
                Field = new FormFieldDescriptor
                {
                    Key = "slug",
                    Title = "slug",
                    Type = FormFieldType.String,
                    Editor = "core.input.url",
                    Rules = [new FormRuleDefinition { Type = FormRuleCatalog.Unique }],
                },
            },
        };

        var item = _normalizer.Normalize(saved, defaults).Single();

        item.Visible.Should().BeFalse("настройки представления берутся из раскладки");
        item.Field!.Editor.Should().Be("core.input.url");
        item.Field.Rules.Select(r => r.Type).Should().Equal(FormRuleCatalog.Unique);
    }

    [Fact]
    public void Structure_IsKept_WithParentsAndInheritedZone()
    {
        var saved = new List<FormItem>
        {
            new() { Key = "tab-1", Zone = "main", Kind = FormItemKind.Container, Title = "Основное" },
            new() { Key = "row-1", Parent = "tab-1", Kind = FormItemKind.Row },
            new() { Key = "col-1", Parent = "row-1", Kind = FormItemKind.Column, Width = FormItemWidths.Half },
            new() { Key = "col-2", Parent = "row-1", Kind = FormItemKind.Column, Width = FormItemWidths.Half },
            new() { Key = "title", Parent = "col-1" },
        };

        var result = _normalizer.Normalize(saved, Defaults());

        result.Select(i => i.Key).Should().Equal("tab-1", "row-1", "col-1", "col-2", "title", "slug", "status");
        result.Select(i => i.Kind).Should().Equal(
            FormItemKind.Container, FormItemKind.Row, FormItemKind.Column, FormItemKind.Column,
            FormItemKind.Field, FormItemKind.Field, FormItemKind.Field);
        result.Select(i => i.Parent).Should().Equal(null, "tab-1", "row-1", "row-1", "col-1", null, null);
        result.Select(i => i.Zone).Should().Equal(
            "main", "main", "main", "main", "main", "main", "side");
        result.Single(i => i.Key == "title").Field.Should().NotBeNull("дескриптор берётся у провайдера");

        var tree = FormLayoutTree.Build(result, "main");
        tree.Select(n => n.Item.Key).Should().Equal("tab-1", "slug");
        tree[0].Children.Single().Children.Select(c => c.Item.Key).Should().Equal("col-1", "col-2");
        tree[0].Children.Single().Children[1].Children.Should().BeEmpty("колонка может быть пустой");
    }

    [Fact]
    public void ChildWithDisallowedParent_GoesToZoneRoot()
    {
        var saved = new List<FormItem>
        {
            new() { Key = "title", Zone = "main" },
            new() { Key = "slug", Parent = "title" },
        };

        var result = _normalizer.Normalize(saved, Defaults());

        var slug = result.Single(i => i.Key == "slug");
        slug.Parent.Should().BeNull("поле не может держать детей");
        slug.Zone.Should().Be("main");
    }

    [Fact]
    public void ChildWithMissingParent_GoesToZoneRoot()
    {
        var result = _normalizer.Normalize([new FormItem { Key = "status", Parent = "ghost", Zone = "side" }],
                                           Defaults());

        result.Single(i => i.Key == "status").Parent.Should().BeNull();
    }

    [Fact]
    public void ColumnWithBrokenParent_IsDropped_ItsFieldGoesToZoneRoot()
    {
        var saved = new List<FormItem>
        {
            new() { Key = "orphan-col", Parent = "ghost", Kind = FormItemKind.Column, Zone = "main" },
            new() { Key = "title", Parent = "orphan-col" },
        };

        var result = _normalizer.Normalize(saved, Defaults());

        result.Select(i => i.Key).Should().Equal("title", "slug", "status");
        result.First().Parent.Should().BeNull();
    }

    [Fact]
    public void CyclicPlacement_BreaksIntoZoneRoots()
    {
        var saved = new List<FormItem>
        {
            new() { Key = "row-a", Zone = "main", Kind = FormItemKind.Row, Parent = "row-b" },
            new() { Key = "row-b", Zone = "main", Kind = FormItemKind.Row, Parent = "row-a" },
        };

        var result = _normalizer.Normalize(saved, Defaults());

        result.Where(i => i.Key.StartsWith("row-")).Should().OnlyContain(i => i.Parent == null);
    }

    [Fact]
    public void ParentAfterChild_InList_StillAttaches()
    {
        var saved = new List<FormItem>
        {
            new() { Key = "title", Parent = "row-1" },
            new() { Key = "row-1", Zone = "main", Kind = FormItemKind.Row },
        };

        var result = _normalizer.Normalize(saved, Defaults());

        result.Single(i => i.Key == "title").Parent.Should().Be("row-1", "порядок узлов в списке не важен");
    }

    [Fact]
    public void StructureNodeWithFieldKey_IsDropped_FieldSurvives()
    {
        var saved = new List<FormItem>
        {
            new() { Key = "title", Zone = "main", Kind = FormItemKind.Container, Title = "Лишний таб" },
        };

        var result = _normalizer.Normalize(saved, Defaults());

        result.Select(i => i.Key).Should().Equal("title", "slug", "status");
        result.First().Kind.Should().Be(FormItemKind.Field);
        result.First().Field.Should().NotBeNull();
    }

    [Fact]
    public void Normalize_IsIdempotent()
    {
        var saved = new List<FormItem>
        {
            new() { Key = "tab-1", Zone = "main", Kind = FormItemKind.Container, Title = "Основное" },
            new() { Key = "row-1", Parent = "tab-1", Kind = FormItemKind.Row },
            new() { Key = "col-1", Parent = "row-1", Kind = FormItemKind.Column, Width = FormItemWidths.Half },
            new() { Key = "group-1", Zone = "main", Kind = FormItemKind.Heading, Title = "Основное" },
            new() { Key = "slug", Parent = "col-1", Width = FormItemWidths.Half, Visible = false },
            new() { Key = "ghost", Zone = "side" },
            new() { Key = "orphan-col", Parent = "ghost", Kind = FormItemKind.Column },
            new() { Key = "status-in-column", Parent = "orphan-col" },
        };

        var once = _normalizer.Normalize(saved, Defaults());
        var twice = _normalizer.Normalize(once, Defaults());

        twice.Should().BeEquivalentTo(once);
    }

    static IEnumerable<string> KeysOf(IEnumerable<FormItem> items, string zone)
        => items.Where(i => i.Zone == zone && i.Field is not null).Select(i => i.Key);

    static IReadOnlyCollection<FormItem> Defaults() =>
    [
        Field("title", "main"),
        Field("slug", "main"),
        Field("status", "side"),
    ];

    static FormItem Field(string key, string zone) => new()
    {
        Key = key,
        Zone = zone,
        Field = new FormFieldDescriptor
        {
            Key = key,
            Title = key,
            Type = FormFieldType.String,
        },
    };
}
