using FluentAssertions;
using Mars.Nodes.Workspace.Services.ValueFields;

namespace Mars.Nodes.Tests.ValueFields;

public class DebugSnapshotValuesTests
{
    [Fact]
    public void Flatten_Scalars_ReturnsValuesAsText()
    {
        var values = DebugSnapshotValues.Flatten("""{"Payload":"hello","count":42,"flag":true}""");

        values.Should().Equal(new Dictionary<string, string>
        {
            ["Payload"] = "hello",
            ["count"] = "42",
            ["flag"] = "True",
        });
    }

    [Fact]
    public void Flatten_NestedObject_UsesDottedPaths()
    {
        var values = DebugSnapshotValues.Flatten("""{"user":{"email":"a@b.c"}}""");

        values.Should().ContainKey("user.email").WhoseValue.Should().Be("a@b.c");
    }

    [Fact]
    public void Flatten_Array_AddsItemsAndSummary()
    {
        var values = DebugSnapshotValues.Flatten("""{"items":[{"name":"one"},{"name":"two"}]}""");

        values.Should().ContainKey("items[0].name").WhoseValue.Should().Be("one");
        values.Should().ContainKey("items[1].name").WhoseValue.Should().Be("two");
        values.Should().ContainKey("items").WhoseValue.Should().Be("[2 items]");
    }

    [Fact]
    public void Flatten_LongString_IsShortened()
    {
        var values = DebugSnapshotValues.Flatten($$"""{"Payload":"{{new string('a', 200)}}"}""");

        values["Payload"].Length.Should().Be(DebugSnapshotValues.MaxValueLength + 3);
    }

    [Fact]
    public void Flatten_InvalidJson_ReturnsEmpty()
    {
        DebugSnapshotValues.Flatten("not json").Should().BeEmpty();
    }
}
