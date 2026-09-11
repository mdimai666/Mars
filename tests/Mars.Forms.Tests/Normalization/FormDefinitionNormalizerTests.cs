using FluentAssertions;
using Mars.Forms.Abstractions.Services;
using Mars.Forms.Contracts;

namespace Mars.Forms.Tests.Normalization;

/// <summary>
/// Раскладка приводится к действующей: сохранённый порядок и настройки выигрывают, неизвестные
/// ключи отбрасываются, недостающие поля провайдера дописываются, а элементы живут только
/// в колонках — свободные (легаси-плоская раскладка) оборачиваются в ряд с колонками.
/// </summary>
public class FormDefinitionNormalizerTests
{
    readonly FormDefinitionNormalizer _normalizer = new();

    [Fact]
    public void NoSavedLayout_GivesEachZoneOneRowWithAllFieldsInOneColumn()
    {
        var result = _normalizer.Normalize(null, Defaults());

        Keys(result).Should().Equal("title", "slug", "status");
        FieldsInZone(result, "main").Should().Equal("title", "slug");
        FieldsInZone(result, "side").Should().Equal("status");
        Nodes(result, FormItemKind.Row).Should().HaveCount(2, "раскладка по умолчанию — одна строка на зону");
        Nodes(result, FormItemKind.Column).Should().HaveCount(2, "и одна колонка: все поля зоны живут в ней");
        ParentKey(result, "slug").Should().Be(ParentKey(result, "title"));
        ParentKey(result, "status").Should().NotBe(ParentKey(result, "title"));
        Column(result, "title").Width.Should().BeNull("колонка по умолчанию — во всю ширину");
        Links(result);
    }

    [Fact]
    public void WidthlessNeighbour_JoinsSharedColumn_ElementWithWidthGetsItsOwn()
    {
        var saved = new List<FormItem>
        {
            new() { Key = "title", Zone = "main" },
            new() { Key = "slug", Zone = "main", Width = FormItemWidths.Half },
            new() { Key = "status", Zone = "main" },
        };

        var result = _normalizer.Normalize(saved, Defaults());

        Nodes(result, FormItemKind.Row).Should().HaveCount(1, "свободные элементы зоны живут в одном ряду");
        Nodes(result, FormItemKind.Column).Should().HaveCount(3);
        ParentKey(result, "slug").Should().NotBe(ParentKey(result, "title"), "своя ширина — своя колонка");
        ParentKey(result, "status").Should().NotBe(ParentKey(result, "slug"));
        Column(result, "slug").Width.Should().Be(FormItemWidths.Half, "ширина переезжает на колонку");
        Links(result);
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

        Keys(result).Should().Equal("status", "slug", "title");
        FieldsInZone(result, "main").Should().Equal("slug", "title");
        FieldsInZone(result, "side").Should().Equal("status");
        Links(result);
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

        Keys(result).Should().Equal("title", "slug", "status");
        Links(result);
    }

    [Fact]
    public void SavedItemWithoutZone_FallsBackToDefaultZone()
    {
        var result = _normalizer.Normalize([new FormItem { Key = "status" }], Defaults());

        Field(result, "status").Zone.Should().Be("side");
    }

    [Fact]
    public void FieldMovedToAnotherZone_StaysThere_AndIsNotDuplicated()
    {
        var saved = new List<FormItem> { new() { Key = "status", Zone = "main" } };

        var result = _normalizer.Normalize(saved, Defaults());

        FieldsInZone(result, "main").Should().Equal("status", "title", "slug");
        FieldsInZone(result, "side").Should().BeEmpty();
        Links(result);
    }

    [Fact]
    public void HeadingMarker_IsKeptOnce_WithItsTitleAndNoField()
    {
        var saved = new List<FormItem>
        {
            new() { Key = "group-1", Zone = "main", Kind = FormItemKind.Heading, Title = "Основное" },
            new() { Key = "title", Zone = "main" },
            new() { Key = "group-1", Zone = "main", Kind = FormItemKind.Heading, Title = "Дубль" },
        };

        var result = _normalizer.Normalize(saved, Defaults());

        Keys(result).Should().Equal("group-1", "title", "slug", "status");

        var heading = Node(result, "group-1");
        heading.Kind.Should().Be(FormItemKind.Heading);
        heading.Title.Should().Be("Основное");
        heading.Field.Should().BeNull();
        heading.Zone.Should().Be("main");
        ParentKey(result, "group-1").Should().StartWith("column-", "заголовок тоже элемент в колонке");
        Links(result);
    }

    [Fact]
    public void HeadingWithoutZone_GetsFirstZone()
    {
        var result = _normalizer.Normalize(
            [new FormItem { Key = "group-1", Kind = FormItemKind.Heading, Title = "Основное" }], Defaults());

        Node(result, "group-1").Zone.Should().Be("main");
    }

    [Fact]
    public void LayoutSettings_KeepVisibleAndTitle_WidthMovesToColumn()
    {
        var saved = new List<FormItem>
        {
            new() { Key = "title", Zone = "main", Visible = false, Width = FormItemWidths.Third, Title = "Заголовок" },
        };

        var result = _normalizer.Normalize(saved, Defaults());

        Field(result, "title").Visible.Should().BeFalse();
        Field(result, "title").Title.Should().Be("Заголовок");
        Column(result, "title").Width.Should().Be(FormItemWidths.Third, "ширина переезжает на колонку");
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

        Field(result, "title").Field!.Title.Should().Be("title");
        Field(result, "title").Field!.Type.Should().Be(FormFieldType.String);
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

        var slug = Field(_normalizer.Normalize(saved, defaults), "slug");

        slug.Visible.Should().BeFalse("настройки представления берутся из раскладки");
        slug.Field!.Editor.Should().Be("core.input.url");
        slug.Field.Rules.Select(rule => rule.Type).Should().Equal(FormRuleCatalog.Unique);
    }

    [Fact]
    public void LooseElements_AreWrappedIntoRowsAndColumns()
    {
        var saved = new List<FormItem>
        {
            new() { Key = "title", Zone = "main", Width = FormItemWidths.Half },
            new() { Key = "slug", Zone = "main", Width = FormItemWidths.Half },
            new() { Key = "status", Zone = "side" },
        };

        var result = _normalizer.Normalize(saved, Defaults());

        // половина + половина встают в один ряд, целое — в свой
        Nodes(result, FormItemKind.Row).Should().HaveCount(2);
        Nodes(result, FormItemKind.Column).Should().HaveCount(3);

        Row(result, "slug").Key.Should().Be(Row(result, "title").Key, "половинки делят ряд");
        ParentKey(result, "slug").Should().NotBe(ParentKey(result, "title"), "но колонка у каждого своя");
        Column(result, "title").Width.Should().Be(FormItemWidths.Half);
        Column(result, "status").Width.Should().BeNull("ширина по умолчанию — во всю ширину");
        Row(result, "status").Parent.Should().BeNull("ряд одиночного элемента стоит в корне зоны");
        Links(result);
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

        ParentKey(result, "row-1").Should().Be("tab-1");
        ParentKey(result, "col-2").Should().Be("row-1");
        ParentKey(result, "title").Should().Be("col-1");
        result.Should().OnlyContain(item => item.Parent != "col-2", "пустая колонка остаётся пустой");
        Node(result, "title").Zone.Should().Be("main", "зона наследуется от контейнера");
        Node(result, "slug").Zone.Should().Be("main");
        Node(result, "status").Zone.Should().Be("side");
        Links(result);

        // дописанные поля тоже встают в колонку
        ParentKey(result, "slug").Should().StartWith("column-");
        ParentKey(result, "status").Should().StartWith("column-");
    }

    [Fact]
    public void ChildWithDisallowedParent_IsWrappedIntoColumn()
    {
        var saved = new List<FormItem>
        {
            new() { Key = "title", Zone = "main" },
            new() { Key = "slug", Parent = "title" },
        };

        var result = _normalizer.Normalize(saved, Defaults());

        Node(result, "slug").Zone.Should().Be("main");
        ParentKey(result, "slug").Should().StartWith("column-", "поле не может держать детей");
        Links(result);
    }

    [Fact]
    public void ChildWithMissingParent_IsWrappedIntoColumn()
    {
        var result = _normalizer.Normalize([new FormItem { Key = "status", Parent = "ghost", Zone = "side" }],
                                           Defaults());

        Node(result, "status").Zone.Should().Be("side");
        ParentKey(result, "status").Should().StartWith("column-");
        Links(result);
    }

    [Fact]
    public void ColumnWithBrokenParent_IsDropped_ItsFieldIsWrapped()
    {
        var saved = new List<FormItem>
        {
            new() { Key = "orphan-col", Parent = "ghost", Kind = FormItemKind.Column, Zone = "main" },
            new() { Key = "title", Parent = "orphan-col" },
        };

        var result = _normalizer.Normalize(saved, Defaults());

        Keys(result).Should().StartWith(["title", "slug", "status"]);
        ParentKey(result, "title").Should().StartWith("column-");
        Links(result);
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

        result.Where(item => item.Key.StartsWith("row-")).Should().OnlyContain(item => item.Parent == null);
        Links(result);
    }

    [Fact]
    public void ParentAfterChild_InList_StillAttaches()
    {
        var saved = new List<FormItem>
        {
            new() { Key = "title", Parent = "col-1" },
            new() { Key = "col-1", Kind = FormItemKind.Column, Parent = "row-1" },
            new() { Key = "row-1", Zone = "main", Kind = FormItemKind.Row },
        };

        var result = _normalizer.Normalize(saved, Defaults());

        ParentKey(result, "title").Should().Be("col-1", "порядок узлов в списке не важен");
        ParentKey(result, "col-1").Should().Be("row-1");
        Links(result);
    }

    [Fact]
    public void StructureNodeWithFieldKey_IsDropped_FieldSurvives()
    {
        var saved = new List<FormItem>
        {
            new() { Key = "title", Zone = "main", Kind = FormItemKind.Container, Title = "Лишний таб" },
        };

        var result = _normalizer.Normalize(saved, Defaults());

        Keys(result).Should().Equal("title", "slug", "status");
        Field(result, "title").Field.Should().NotBeNull();
        Nodes(result, FormItemKind.Container).Should().BeEmpty();
        Links(result);
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
        Links(once);
    }

    //=====================================
    // помощники: тесты смотрят на раскладку как на дерево

    /// <summary>Каждый узел лежит там, где ему можно лежать по матрице правил</summary>
    static void Links(IReadOnlyCollection<FormItem> items)
    {
        var byKey = items.ToDictionary(item => item.Key, StringComparer.Ordinal);

        foreach (var item in items.Where(item => item.Parent is not null))
        {
            byKey.Should().ContainKey(item.Parent!, $"{item.Key} ссылается на родителя");
            FormLayoutRules.CanContain(byKey[item.Parent!].Kind, item.Kind).Should()
                             .BeTrue($"{item.Key} ({item.Kind}) не может лежать в {item.Parent}");
        }
    }

    static IReadOnlyCollection<FormItem> Nodes(IEnumerable<FormItem> items, FormItemKind kind)
        => items.Where(item => item.Kind == kind).ToList();

    /// <summary>Ключи неструктурных узлов: элементы раскладки в порядке отображения</summary>
    static IEnumerable<string> Keys(IEnumerable<FormItem> items)
        => items.Where(item => FormLayoutRules.IsElement(item.Kind)).Select(item => item.Key);

    static IEnumerable<string> FieldsInZone(IEnumerable<FormItem> items, string zone)
        => items.Where(item => item.Field is not null && item.Zone == zone).Select(item => item.Key);

    static FormItem Node(IEnumerable<FormItem> items, string? key)
        => items.Single(item => item.Key == key);

    static FormItem Field(IEnumerable<FormItem> items, string key) => Node(items, key);

    static string? ParentKey(IEnumerable<FormItem> items, string key) => Node(items, key).Parent;

    static FormItem Parent(IEnumerable<FormItem> items, string key) => Node(items, ParentKey(items, key));

    /// <summary>Колонка элемента — его родитель</summary>
    static FormItem Column(IEnumerable<FormItem> items, string key) => Parent(items, key);

    /// <summary>Ряд элемента — родитель его колонки</summary>
    static FormItem Row(IEnumerable<FormItem> items, string key) => Parent(items, Column(items, key).Key);

    static IReadOnlyCollection<FormItem> Defaults() =>
    [
        FieldItem("title", "main"),
        FieldItem("slug", "main"),
        FieldItem("status", "side"),
    ];

    static FormItem FieldItem(string key, string zone) => new()
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
