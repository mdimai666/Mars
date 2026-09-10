using System.Text.Json.Nodes;
using FluentAssertions;
using Mars.Cms.Abstractions.Dto.MetaFields;

namespace Mars.Server.Tests.Utils;

public class MetaFieldQueryDefinitionTests
{
    [Fact]
    public void ToOptions_FromOptions_Roundtrip()
    {
        var def = new MetaFieldQueryDefinition
        {
            Target = "Post.comment",
            BackReferenceKey = "author",
            Filter = "some filter",
        };

        var parsed = MetaFieldQueryDefinition.FromOptions(def.ToOptions());

        parsed.Should().Be(def);
    }

    [Fact]
    public void FromOptions_NoFilter_FilterIsNull()
    {
        var def = new MetaFieldQueryDefinition
        {
            Target = "Post.comment",
            BackReferenceKey = "author",
        };

        var parsed = MetaFieldQueryDefinition.FromOptions(def.ToOptions());

        parsed.Should().NotBeNull();
        parsed!.Filter.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("{}")]
    [InlineData("""{ "target": "Post.comment" }""")]
    [InlineData("""{ "backReferenceKey": "author" }""")]
    public void FromOptions_IncompleteOptions_ReturnsNull(string? json)
    {
        var options = json is null ? null : JsonNode.Parse(json);

        MetaFieldQueryDefinition.FromOptions(options).Should().BeNull();
    }
}
