using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Host.Shared;
using Mars.Nodes.Implements.Test.NodesForTesting;
using NSubstitute;

namespace Mars.Nodes.Implements.Test.Services;

public class NodeLifecycleTests : NodeServiceUnitTestBase
{
    private FlowNodeImpl _flow;
    private NodeLifecycleTestNodeImpl _nodeImplement;
    private Node[] _nodes;

    public NodeLifecycleTests()
    {
        _flow = new FlowNodeImpl(new(), null!);
        _flow.RNS = Runtime.CreateContextForNode(_flow.Node, _flow);
        var node = new NodeLifecycleTestNode
        {
            Container = _flow.Node.Id
        };
        _nodeImplement = Substitute.ForPartsOf<NodeLifecycleTestNodeImpl>(node, Runtime.CreateContextForNode(node, _flow));

        _nodeImplementFactory.Create(Arg.Is<INodeBasic>(s => s.Id == _flow.Node.Id), Arg.Any<IRuntimeNodeScope>()).Returns(_flow);
        _nodeImplementFactory.Create(Arg.Is<INodeBasic>(s => s.Id == node.Id), Arg.Any<IRuntimeNodeScope>()).Returns(_nodeImplement);

        _nodes = [_nodeImplement.Node, _flow.Node];
    }

    [Fact]
    public async Task Deploy_OnNodeAssigned_ShouldCallAfterAssign()
    {
        //Arrange

        //Act
        _nodeService.Deploy(_nodes);

        //Assert
        await _nodeImplement.Received(1).OnNodeAssigned(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deploy_OnNodeDelete_ShouldCallAfterDelete()
    {
        //Arrange
        _nodeService.Deploy(_nodes);

        //Act
        _nodeService.Deploy([]);

        //Assert
        await _nodeImplement.Received(1).OnNodeDelete();
    }

    [Fact]
    public async Task Deploy_Dispose_ShouldCallOnRecreate()
    {
        //Arrange
        _nodeService.Deploy(_nodes);

        //Act
        _nodeService.Deploy([]);

        //Assert
        _nodeImplement.Received(1).Dispose();
        await _nodeImplement.Received(1).DisposeAsync();
    }
}
