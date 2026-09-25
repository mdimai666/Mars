using FluentAssertions;
using Mars.Nodes.Core.Nodes.Common;
using Mars.Nodes.Core.Nodes.Functions;
using Mars.Nodes.Workspace.Models;

namespace Mars.Nodes.Tests.Models;

public class PaletteBuilderTests
{
    [Fact]
    public void Build_TopNodesFirst_AndRegisteredDeduplicated()
    {
        //Act
        var palette = PaletteBuilder.Build(
            [typeof(FunctionNode), typeof(InjectNode), typeof(CommentNode)],
            new Dictionary<string, InlineFunctionNodeSchema>());

        //Assert
        palette.Should().HaveCount(5);
        palette.Select(s => s.Instance.GetType()).Should().ContainInOrder(
            typeof(InjectNode), typeof(DebugNode), typeof(FunctionNode), typeof(TemplateNode), typeof(CommentNode));
    }

    [Fact]
    public void Build_FiltersUnknownAndNonVisualNodes()
    {
        //Act
        var palette = PaletteBuilder.Build(
            [typeof(UnknownNode), typeof(VarNode), typeof(FlowNode), typeof(CommentNode)],
            new Dictionary<string, InlineFunctionNodeSchema>());

        //Assert
        var types = palette.Select(s => s.Instance.GetType()).ToList();
        types.Should().NotContain(typeof(UnknownNode));
        types.Should().NotContain(typeof(VarNode));
        types.Should().NotContain(typeof(FlowNode));
        palette[^1].Instance.Should().BeOfType<CommentNode>();
    }

    [Fact]
    public void Build_GroupNameFromDisplayAttribute()
    {
        //Act
        var palette = PaletteBuilder.Build(
            [typeof(CommentNode)],
            new Dictionary<string, InlineFunctionNodeSchema>());

        //Assert
        palette.Single(s => s.Instance is CommentNode).GroupName.Should().Be("common");
        palette.Should().OnlyContain(s => !string.IsNullOrEmpty(s.GroupName));
    }

    [Fact]
    public void Build_AppendsInlineFunctionNodes()
    {
        //Arrange
        var schema = new InlineFunctionNodeSchema
        {
            TypeId = "test.InlineFn",
            Name = "TestInline",
            Color = null,
            Icon = null,
            GroupName = "test-group",
            Inputs = [],
            Outputs = [],
            Parameters = []
        };

        //Act
        var palette = PaletteBuilder.Build([], new Dictionary<string, InlineFunctionNodeSchema> { [schema.TypeId] = schema });

        //Assert
        palette.Should().HaveCount(5);
        var last = palette[^1];
        var inline = last.Instance.Should().BeOfType<InlineFunctionNode>().Subject;
        inline.FunctionId.Should().Be("test.InlineFn");
        inline.Name.Should().Be("TestInline");
        last.GroupName.Should().Be("test-group");
    }
}
