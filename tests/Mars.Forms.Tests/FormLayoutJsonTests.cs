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
                new FormItem { Key = "section-1", Zone = "main", SectionTitle = "Основное" },
                new FormItem { Key = "title", Zone = "main", Width = FormItemWidths.Half },
                new FormItem { Key = "slug", Zone = "main", Visible = false },
            ],
        };

        var parsed = FormLayoutJson.Parse(settings.ToJsonNode());

        parsed.Should().NotBeNull();
        parsed!.Items.Select(i => i.Key).Should().Equal("section-1", "title", "slug");

        var marker = parsed.Items.First();
        marker.IsSectionHeader.Should().BeTrue();
        marker.SectionTitle.Should().Be("Основное");
        marker.Field.Should().BeNull();

        parsed.Items.ElementAt(1).Width.Should().Be(FormItemWidths.Half);
        parsed.Items.Last().Visible.Should().BeFalse();
    }

    [Fact]
    public void Serialize_UsesCamelCase()
    {
        var settings = new FormLayoutSettings
        {
            Items = [new FormItem { Key = "title", Zone = "main", Width = FormItemWidths.Full }],
        };

        var item = settings.ToJsonNode()!["items"]![0]!.AsObject();

        item.ContainsKey("zone").Should().BeTrue();
        item.ContainsKey("width").Should().BeTrue();
        item.ContainsKey("Zone").Should().BeFalse();
    }

    [Fact]
    public void Parse_NullOrBrokenJson_ReturnsNull()
    {
        FormLayoutJson.Parse(null).Should().BeNull();
        FormLayoutJson.Parse(JsonNode.Parse("""{ "items": "not-an-array" }""")).Should().BeNull();
        ((FormLayoutSettings?)null).ToJsonNode().Should().BeNull();
    }

    [Fact]
    public void Parse_LegacySectionTree_BecomesFlatMarkers()
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
        parsed.Items.ElementAt(1).IsSectionHeader.Should().BeTrue();
        parsed.Items.ElementAt(1).SectionTitle.Should().Be("Основное");
        parsed.Items.ElementAt(2).Zone.Should().Be("main", "дети секции наследуют её зону");
        parsed.Items.ElementAt(2).Width.Should().Be(FormItemWidths.Half);
        parsed.Items.ElementAt(3).SectionTitle.Should().Be("Вложенная");
    }

    [Fact]
    public void ToLayout_StripsDescriptors_TheyAreNeverStored()
    {
        var items = new List<FormItem>
        {
            new() { Key = "section-1", Zone = "main", SectionTitle = "Основное" },
            new()
            {
                Key = "title",
                Zone = "main",
                Field = new FormFieldDescriptor { Key = "title", Title = "Заголовок", Type = FormFieldType.String },
            },
        };

        var layout = items.ToLayout();

        layout.Items.Should().OnlyContain(i => i.Field == null);
        layout.Items.Select(i => i.Key).Should().Equal("section-1", "title");
    }

    [Fact]
    public void RuleDefinition_FromJson_SkipsBrokenEntries()
    {
        var node = JsonNode.Parse("""[{"type":"regex","params":{"pattern":"^a"}},{"type":""},"not-an-object"]""");

        var rules = FormRuleDefinition.FromJson(node);

        rules.Should().ContainSingle().Which.Type.Should().Be(FormRuleCatalog.Regex);
    }
}
