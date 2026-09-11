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
    public void Normalize_IsIdempotent()
    {
        var saved = new List<FormItem>
        {
            new() { Key = "group-1", Zone = "main", Kind = FormItemKind.Heading, Title = "Основное" },
            new() { Key = "slug", Zone = "main", Width = FormItemWidths.Half, Visible = false },
            new() { Key = "ghost", Zone = "side" },
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
