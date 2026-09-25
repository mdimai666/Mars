using System.Text.Json.Nodes;
using FluentAssertions;
using Mars.Forms.Contracts;

namespace Mars.Forms.Tests;

public class FormLayoutJsonTests
{
    [Fact]
    public void RoundTrip_PreservesLayout()
    {
        var settings = new FormLayoutSettings
        {
            Items =
            [
                new FormItem { Key = "heading-1", Zone = "main", Kind = FormItemKind.Heading, Title = "Основное" },
                new FormItem { Key = "title", Zone = "main", Width = FormItemWidths.Half },
                new FormItem { Key = "slug", Zone = "main", Visible = false },
            ],
        };

        var parsed = FormLayoutJson.Parse(settings.ToJsonNode());

        parsed.Should().NotBeNull();
        parsed!.Items.Select(i => i.Key).Should().Equal("heading-1", "title", "slug");

        var heading = parsed.Items.First();
        heading.Kind.Should().Be(FormItemKind.Heading);
        heading.Title.Should().Be("Основное");
        heading.Field.Should().BeNull();

        parsed.Items.ElementAt(1).Width.Should().Be(FormItemWidths.Half);
        parsed.Items.Last().Visible.Should().BeFalse();
    }

    [Fact]
    public void RoundTrip_PreservesKindAndParent()
    {
        var settings = new FormLayoutSettings
        {
            Items =
            [
                new FormItem { Key = "tab-1", Zone = "main", Kind = FormItemKind.Container, Title = "Основное" },
                new FormItem { Key = "row-1", Parent = "tab-1", Kind = FormItemKind.Row },
                new FormItem { Key = "col-1", Parent = "row-1", Kind = FormItemKind.Column, Width = FormItemWidths.Third },
                new FormItem { Key = "title", Parent = "col-1", Width = FormItemWidths.Half },
                new FormItem { Key = "div-1", Parent = "col-1", Kind = FormItemKind.Divider },
            ],
        };

        var parsed = FormLayoutJson.Parse(settings.ToJsonNode());

        parsed!.Items.Select(i => i.Kind).Should().Equal(
            FormItemKind.Container, FormItemKind.Row, FormItemKind.Column, FormItemKind.Field, FormItemKind.Divider);
        parsed.Items.Select(i => i.Parent).Should().Equal(null, "tab-1", "row-1", "col-1", "col-1");
        parsed.Items.ElementAt(2).Width.Should().Be(FormItemWidths.Third);
    }

    [Fact]
    public void Serialize_UsesCamelCase_AndStringKinds()
    {
        var settings = new FormLayoutSettings
        {
            Items =
            [
                new FormItem { Key = "title", Zone = "main", Width = FormItemWidths.Full },
                new FormItem { Key = "row-1", Zone = "main", Kind = FormItemKind.Row },
            ],
        };

        var items = settings.ToJsonNode()!["items"]!.AsArray();
        var item = items[0]!.AsObject();

        item.ContainsKey("zone").Should().BeTrue();
        item.ContainsKey("width").Should().BeTrue();
        item.ContainsKey("Zone").Should().BeFalse();
        item.ContainsKey("kind").Should().BeFalse("тип поля — значение по умолчанию");
        item.ContainsKey("field").Should().BeFalse("дескрипторы не хранятся");

        items[1]!.AsObject()["kind"]!.GetValue<string>().Should().Be("row");
    }

    [Fact]
    public void Parse_NullOrBrokenJson_ReturnsNull()
    {
        FormLayoutJson.Parse(null).Should().BeNull();
        FormLayoutJson.Parse(JsonNode.Parse("""{ "items": "not-an-array" }""")).Should().BeNull();
        ((FormLayoutSettings?)null).ToJsonNode().Should().BeNull();
    }

    [Fact]
    public void Parse_LegacyFlatLayout_IsFieldWithoutParent()
    {
        var node = JsonNode.Parse("""
        {
          "items": [
            { "key": "title", "zone": "main", "width": "half" },
            { "key": "section-1", "zone": "main", "sectionTitle": "Прежняя секция" }
          ]
        }
        """);

        var parsed = FormLayoutJson.Parse(node);

        parsed!.Items.First().Kind.Should().Be(FormItemKind.Field);
        parsed.Items.First().Parent.Should().BeNull();
        parsed.Items.First().Width.Should().Be(FormItemWidths.Half);

        parsed.Items.Last().Kind.Should().Be(FormItemKind.Heading);
        parsed.Items.Last().Title.Should().Be("Прежняя секция");
    }

    [Fact]
    public void Parse_LegacySectionTree_BecomesFlatHeadings()
    {
        var node = JsonNode.Parse("""
        {
          "items": [
            { "key": "title", "zone": "main" },
            { "key": "g1", "zone": "main", "title": "Основное", "collapsed": true,
              "items": [
                { "key": "slug", "width": "half" },
                { "key": "g2", "title": "Вложенная", "items": [ { "key": "status" } ] }
              ] }
          ]
        }
        """);

        var parsed = FormLayoutJson.Parse(node);

        parsed!.Items.Select(i => i.Key).Should().Equal("title", "g1", "slug", "g2", "status");
        parsed.Items.ElementAt(1).Kind.Should().Be(FormItemKind.Heading);
        parsed.Items.ElementAt(1).Title.Should().Be("Основное");
        parsed.Items.ElementAt(2).Zone.Should().Be("main", "дети секции наследуют её зону");
        parsed.Items.ElementAt(2).Width.Should().Be(FormItemWidths.Half);
        parsed.Items.ElementAt(3).Title.Should().Be("Вложенная");
        parsed.Items.Should().OnlyContain(i => i.Parent == null, "старое дерево разворачивалось в плоский список");
    }

    [Fact]
    public void ToLayout_StripsDescriptors_TheyAreNeverStored()
    {
        var items = new List<FormItem>
        {
            new() { Key = "heading-1", Zone = "main", Kind = FormItemKind.Heading, Title = "Основное" },
            new()
            {
                Key = "title",
                Zone = "main",
                Field = new FormFieldDescriptor { Key = "title", Title = "Заголовок", Type = FormFieldType.String },
            },
        };

        var layout = items.ToLayout();

        layout.Items.Should().OnlyContain(i => i.Field == null);
        layout.Items.Select(i => i.Key).Should().Equal("heading-1", "title");
        layout.Items.First().Kind.Should().Be(FormItemKind.Heading);
    }

    [Fact]
    public void RuleDefinition_FromJson_SkipsBrokenEntries()
    {
        var node = JsonNode.Parse("""[{"type":"regex","params":{"pattern":"^a"}},{"type":""},"not-an-object"]""");

        var rules = FormRuleDefinition.FromJson(node);

        rules.Should().ContainSingle().Which.Type.Should().Be(FormRuleCatalog.Regex);
    }
}
