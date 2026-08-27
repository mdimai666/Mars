using System.Text.Json.Nodes;
using FluentAssertions;
using Mars.Cms.Abstractions.Dto.PostTypes;
using Mars.Contracts.PostTypes;

namespace Test.Mars.Server.Dto;

public class PostTypeGridSettingsJsonTests
{
    [Fact]
    public void RoundTrip_PreservesSettings()
    {
        var settings = new PostTypeGridSettings
        {
            Columns =
            [
                new PostTypeGridColumn { Key = "title", Visible = true },
                new PostTypeGridColumn { Key = "subtitle", Visible = false },
            ],
            SortKey = "created_at",
            SortDescending = true,
        };

        var node = settings.ToJsonNode();
        var parsed = PostTypeGridSettingsJson.Parse(node);

        parsed.Should().BeEquivalentTo(settings);
    }

    [Fact]
    public void ToJsonNode_NullSettings_ReturnsNull()
    {
        ((PostTypeGridSettings?)null).ToJsonNode().Should().BeNull();
    }

    [Fact]
    public void Parse_NullNode_ReturnsNull()
    {
        PostTypeGridSettingsJson.Parse(null).Should().BeNull();
    }

    [Fact]
    public void Parse_BrokenJson_ReturnsNull()
    {
        var broken = JsonNode.Parse("""{ "columns": "not-an-array" }""");

        PostTypeGridSettingsJson.Parse(broken).Should().BeNull();
    }

    [Fact]
    public void Serialize_UsesCamelCase()
    {
        var settings = new PostTypeGridSettings
        {
            Columns = [new PostTypeGridColumn { Key = "title" }],
            SortKey = "title",
            SortDescending = false,
        };

        var node = settings.ToJsonNode()!.AsObject();

        node.ContainsKey("sortKey").Should().BeTrue();
        node.ContainsKey("columns").Should().BeTrue();
        node.ContainsKey("SortKey").Should().BeFalse();
    }
}
