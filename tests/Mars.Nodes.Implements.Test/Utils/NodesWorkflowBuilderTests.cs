using FluentAssertions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Implements.Test.Utils;

public class NodesWorkflowBuilderTests
{
    [Fact]
    public void AddNext_AddSome_ShouldSuccess()
    {
        //Arrange
        Node[] nodesList = [new InjectNode(), new TemplateNode(), new DebugNode()];

        //Act
        var nodes = NodesWorkflowBuilder.Create()
                                        .AddNext(nodesList[0])
                                        .AddNext(nodesList[1])
                                        .AddNext(nodesList[2])
                                        .Build();

        //Assert
        nodes.Length.Should().Be(3);
        for (var i = 0; i < nodes.Length - 1; i++)
        {
            var node = nodes[i];
            node.Wires.Count.Should().Be(1);
            node.Wires.First().Count.Should().Be(1);
            var next = nodes[i + 1];
            node.Wires.First().First().NodeId.Should().Be(next.Id);
        }
    }

    [Fact]
    public void GetLastGenerationOutputables_ReturnOne()
    {
        //Act
        var builder = NodesWorkflowBuilder.Create()
                                        .AddNext(new InjectNode());

        //Assert
        builder.GetLastGenerationOutputables().Count().Should().Be(1);
    }

    [Fact]
    public void GetLastGenerationOutputables_TryGetWithoutOutputNodes_ShouldException()
    {
        //Act
        var action = () => NodesWorkflowBuilder.Create()
                                        .AddNext(new DebugNode())
                                        .AddNext(new DebugNode());

        //Assert
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddNext_UsingBuilderArgument_ShouldAddNodesCorrectly()
    {
        //Assert
        var injectNode = new InjectNode();
        var template1 = new TemplateNode();
        var debugNode1 = new DebugNode();
        var functionNode1 = new FunctionNode();
        var template22 = new TemplateNode() { Name = "template22" };
        var debug22 = new DebugNode() { Name = "debug22" };
        var debugNode3 = new DebugNode();

        //Act
        var builder = NodesWorkflowBuilder.Create()
                                        .AddNext(injectNode)
                                        .AddNext(NodesWorkflowBuilder.Create()
                                                                    .AddNext(template1)
                                                                    .AddNext(debugNode1, functionNode1),
                                                    NodesWorkflowBuilder.Create()
                                                                    .AddNext(template22)
                                                                    .AddNext(debug22)
                                                )
                                        .AddNext(debugNode3);

        //Assert
        builder.BuilderItems[template1.Id].Generation.Should().Be(1);
        builder.BuilderItems[debugNode1.Id].Generation.Should().Be(2);
        builder.BuilderItems[functionNode1.Id].Generation.Should().Be(2);
        builder.BuilderItems[template22.Id].Generation.Should().Be(1);
        builder.BuilderItems[debug22.Id].Generation.Should().Be(2);
        builder.BuilderItems[debugNode3.Id].Generation.Should().Be(3);

        builder.BuilderItems[injectNode.Id].Node.Wires[0][0].NodeId.Should().Be(template1.Id);
        builder.BuilderItems[functionNode1.Id].Node.Wires[0][0].NodeId.Should().Be(debugNode3.Id);

        builder.BuilderItems[template1.Id].ElementRowIndex.Should().Be(0);
        builder.BuilderItems[debugNode1.Id].ElementRowIndex.Should().Be(0);
        builder.BuilderItems[functionNode1.Id].ElementRowIndex.Should().Be(1);
        builder.BuilderItems[template22.Id].ElementRowIndex.Should().Be(2);
        builder.BuilderItems[debug22.Id].ElementRowIndex.Should().Be(2);
        builder.BuilderItems[debugNode3.Id].ElementRowIndex.Should().Be(0);

    }

}
