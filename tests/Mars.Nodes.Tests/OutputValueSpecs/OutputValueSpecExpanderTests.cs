using System.Text.Json;
using FluentAssertions;
using Mars.Nodes.Core;

namespace Mars.Nodes.Tests.OutputValueSpecs;

public class OutputValueSpecExpanderTests
{
    [Fact]
    public void Expand_Scalar_ReturnsSingleSpec()
    {
        OutputValueSpecExpander.Expand("Payload", typeof(string))
            .Should().Equal(new OutputValueSpec("Payload", "string"));
    }

    [Fact]
    public void Expand_NestedTypeAndArray_ExpandsIntoPaths()
    {
        OutputValueSpecExpander.Expand("Payload", typeof(OrderDto)).Should().Equal(
            new OutputValueSpec("Payload", "object"),
            new OutputValueSpec("Payload.Status", "string"),
            new OutputValueSpec("Payload.Items", "object[]"),
            new OutputValueSpec("Payload.Items[].Name", "string"),
            new OutputValueSpec("Payload.Items[].Qty", "int"),
            new OutputValueSpec("Payload.Owner", "object"),
            new OutputValueSpec("Payload.Owner.Name", "string"),
            new OutputValueSpec("Payload.Owner.Qty", "int"));
    }

    [Fact]
    public void Expand_MaxDepthZero_ReturnsTypeItself()
    {
        OutputValueSpecExpander.Expand("Payload", typeof(OrderDto), 0)
            .Should().Equal(new OutputValueSpec("Payload", "object"));
    }

    [Fact]
    public void Expand_MaxDepthOne_StopsAfterFirstLevel()
    {
        OutputValueSpecExpander.Expand("Payload", typeof(OrderDto), 1).Select(s => s.Path)
            .Should().Equal("Payload", "Payload.Status", "Payload.Items", "Payload.Owner");
    }

    [Fact]
    public void Expand_CircularTypes_StopsOnVisitedType()
    {
        OutputValueSpecExpander.Expand("Payload", typeof(CircularA), 5).Select(s => s.Path)
            .Should().Equal("Payload", "Payload.B", "Payload.B.A");
    }

    [Fact]
    public void Expand_GenericList_UsesArrayName()
    {
        OutputValueSpecExpander.Expand("Payload", typeof(List<string>))
            .Should().Equal(new OutputValueSpec("Payload", "string[]"));
    }

    [Fact]
    public void Expand_OpaqueTypes_AreNotExpanded()
    {
        OutputValueSpecExpander.Expand("Payload", typeof(JsonElement))
            .Should().Equal(new OutputValueSpec("Payload", "object"));

        OutputValueSpecExpander.Expand("Payload", typeof(Dictionary<string, int>))
            .Should().Equal(new OutputValueSpec("Payload", "object"));
    }

    [Fact]
    public void Expand_EmptyPath_ReturnsNothing()
    {
        OutputValueSpecExpander.Expand(" ", typeof(OrderDto)).Should().BeEmpty();
    }

    [Fact]
    public void Expand_OutputPort_StampsEverySpec()
    {
        OutputValueSpecExpander.Expand("Payload", typeof(OrderDto), outputPort: OutputValueSpec.AllOutputPorts)
            .Should().OnlyContain(s => s.OutputPort == OutputValueSpec.AllOutputPorts);
    }

    private sealed class OrderDto
    {
        public string Status { get; set; } = "";
        public OrderItem[] Items { get; set; } = [];
        public OrderItem? Owner { get; set; }
    }

    private sealed class OrderItem
    {
        public string Name { get; set; } = "";
        public int Qty { get; set; }
    }

    private sealed class CircularA
    {
        public CircularB? B { get; set; }
    }

    private sealed class CircularB
    {
        public CircularA? A { get; set; }
    }
}

public class VarNodeTypeNameTests
{
    [Theory]
    [InlineData(typeof(int), "int")]
    [InlineData(typeof(int?), "int")]
    [InlineData(typeof(long), "long")]
    [InlineData(typeof(decimal), "decimal")]
    [InlineData(typeof(string), "string")]
    [InlineData(typeof(string[]), "string[]")]
    [InlineData(typeof(int[][]), "int[][]")]
    [InlineData(typeof(DateTime), "DateTime")]
    [InlineData(typeof(Guid), "Guid")]
    public void GetVarTypeName_MapsClrType(Type type, string expected)
    {
        VarNode.GetVarTypeName(type).Should().Be(expected);
    }

    [Fact]
    public void GetVarTypeName_UnknownTypes_ReturnObject()
    {
        VarNode.GetVarTypeName(null).Should().Be(VarNode.ObjectTypeName);
        VarNode.GetVarTypeName(typeof(object)).Should().Be(VarNode.ObjectTypeName);
        VarNode.GetVarTypeName(typeof(OutputValueSpecExpanderTests)).Should().Be(VarNode.ObjectTypeName);
    }
}
