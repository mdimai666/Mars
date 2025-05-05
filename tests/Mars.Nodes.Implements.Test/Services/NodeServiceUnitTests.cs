using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.Services;
using FluentAssertions;

namespace Mars.Nodes.Implements.Test.Services;

public class NodeServiceUnitTests : NodeServiceUnitTestBase
{
    [Fact]
    public void OrderNodesForInitialize_FirstFlowsAndConfigNodes_ShouldReturnSorted()
    {
        //Arrange
        _ = nameof(NodeService.OrderNodesForInitialize);
        var nodes = new List<Node>
        {
            new InjectNode(),
            new DebugNode(),
            new FlowNode(),
        };

        //Act
        var orderedNodes = NodeService.OrderNodesForInitialize(nodes);

        //Assert
        orderedNodes[0].GetType().Should().Be(typeof(FlowNode));
    }

}
