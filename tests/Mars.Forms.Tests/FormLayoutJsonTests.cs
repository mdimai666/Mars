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
                new FormItem
                {
                    Key = "section-1",
                    Kind = FormItemKinds.Section,
                    Zone = "main",
                    Title = "Основное",
                    Collapsed = true,
                    Items =
                    [
                        new FormItem { Key = "title", Zone = null, Width = FormItemWidths.Half },
                    ],
                },
                new FormItem
                {
                    Key = "slug",
                    Zone = "main",
                    Visible = false,
                    Editor = "core.input.url",
                    Rules = [new FormRuleDefinition { Type = FormRuleCatalog.Regex, Params = new JsonObject { ["pattern"] = "^[a-z]+$" } }],
                },
            ],
        };

        var node = settings.ToJsonNode();
        var parsed = FormLayoutJson.Parse(node);

        parsed.Should().NotBeNull();
        var section = parsed!.Items.First();
        section.Kind.Should().Be(FormItemKinds.Section);
        section.Key.Should().Be("section-1");
        section.Zone.Should().Be("main");
        section.Title.Should().Be("Основное");
        section.Collapsed.Should().BeTrue();
        section.Items.Single().Key.Should().Be("title");
        section.Items.Single().Width.Should().Be(FormItemWidths.Half);

        var slug = parsed.Items.Last();
        slug.Visible.Should().BeFalse();
        slug.Editor.Should().Be("core.input.url");
        slug.Rules.Single().Type.Should().Be(FormRuleCatalog.Regex);
        slug.Rules.Single().Params!["pattern"]!.GetValue<string>().Should().Be("^[a-z]+$");
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
    public void ToLayout_StripsDescriptors_TheyAreNeverStored()
    {
        var items = new List<FormItem>
        {
            new()
            {
                Key = "section-1",
                Kind = FormItemKinds.Section,
                Items =
                [
                    new FormItem
                    {
                        Key = "title",
                        Field = new FormFieldDescriptor { Key = "title", Title = "Заголовок", Type = FormFieldType.String },
                    },
                ],
            },
        };

        var layout = items.ToLayout();

        layout.Items.Should().OnlyContain(i => i.Field == null);
        layout.Items.Single().Items.Single().Field.Should().BeNull();
        layout.Items.Single().Items.Single().Key.Should().Be("title");
    }

    [Fact]
    public void Parse_NullOrBrokenJson_ReturnsNull()
    {
        FormLayoutJson.Parse(null).Should().BeNull();
        FormLayoutJson.Parse(JsonNode.Parse("""{ "items": "not-an-array" }""")).Should().BeNull();
        ((FormLayoutSettings?)null).ToJsonNode().Should().BeNull();
    }

    [Fact]
    public void Parse_StoredRules_SurviveRoundTrip()
    {
        var settings = new FormLayoutSettings
        {
            Items =
            [
                new FormItem
                {
                    Key = "title",
                    Rules = [new FormRuleDefinition { Type = FormRuleCatalog.Length, Params = new JsonObject { ["min"] = 3 } }],
                },
            ],
        };

        var parsed = FormLayoutJson.Parse(settings.ToJsonNode());

        var rule = parsed!.Items.Single().Rules.Single();
        rule.Type.Should().Be(FormRuleCatalog.Length);
        rule.Params!["min"]!.GetValue<int>().Should().Be(3);
    }

    [Fact]
    public void RuleDefinition_FromJson_SkipsBrokenEntries()
    {
        var node = JsonNode.Parse("""[{"type":"regex","params":{"pattern":"^a"}},{"type":""},"not-an-object"]""");

        var rules = FormRuleDefinition.FromJson(node);

        rules.Should().ContainSingle().Which.Type.Should().Be(FormRuleCatalog.Regex);
    }
}
