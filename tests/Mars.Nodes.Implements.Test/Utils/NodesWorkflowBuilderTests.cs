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
}
