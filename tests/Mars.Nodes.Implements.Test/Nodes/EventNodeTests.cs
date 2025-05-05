using FluentAssertions;
using Mars.Host.Shared.Managers;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Implements.Test.NodesForTesting;
using Mars.Nodes.Implements.Test.Services;
using NSubstitute;
using static Mars.Host.Shared.Managers.IEventManager;

namespace Mars.Nodes.Implements.Test.Nodes;

public class EventNodeTests : NodeServiceUnitTestBase
{
    [Fact]
    public async Task Execute_Notify_Success()
    {
        //Arrange
        _ = nameof(EventNodeImpl.Execute);
        var input = new NodeMsg();
        var eventPayload = new ManagerEventPayload("*", 222);
        input.Add(eventPayload);
        var node = new EventNode { Topics = "*" };

        //Act
        var msg = await ExecuteNode(node, input);

        //Assert
        msg.Payload.Should().BeEquivalentTo(eventPayload);
    }

    [Theory]
    [InlineData("entity.post/add", "entity.post/add", true)]
    [InlineData("entity.post/del", "entity.post/add", false)]
    [InlineData("xxx", "entity.post/add", false)]
    [InlineData("entity.post/add", "*", true)]
    [InlineData("entity.post/add", "entity.post/*", true)]
    public async Task TriggerEventNodes_RaiseEventFromNodeService_Success(string triggerTopic, string subscribedTopic, bool expectTouched)
    {
        //Arrange
        _ = nameof(EventNodeImpl.Execute);
        _ = nameof(IEventManager.OnTrigger);

        var input = new NodeMsg();
        var eventPayload = new ManagerEventPayload(triggerTopic, 222);
        input.Add(eventPayload);
        var touchedFlag = false;

        var flowNode = new FlowNode();
        var callbackNode = new TestCallBackNode()
        {
            Container = flowNode.Id,
            Callback = () => touchedFlag = true,
        };
        var node = new EventNode
        {
            Container = flowNode.Id,
            Topics = subscribedTopic,
            Wires = [[callbackNode.Id]]
        };
        _nodeService.Deploy([flowNode, node, callbackNode]);

        //Act
        _eventManager.OnTrigger += Raise.Event<ManagerEventPayloadHandler>(eventPayload);
        await Task.Delay(1);

        //Assert
        touchedFlag.Should().Be(expectTouched);
    }

}
