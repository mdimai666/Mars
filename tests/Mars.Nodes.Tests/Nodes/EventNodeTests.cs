using FluentAssertions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements.Nodes.Events;
using Mars.Nodes.Core.Nodes.Events;
using Mars.Nodes.Tests.NodesForTesting;
using Mars.Nodes.Tests.Services;
using Mars.Server.Abstractions.Managers;
using NSubstitute;
using static Mars.Server.Abstractions.Managers.IEventManager;

namespace Mars.Nodes.Tests.Nodes;

public class EventListenerNodeTests : NodeServiceUnitTestBase
{
    [Fact]
    public async Task Execute_Notify_Success()
    {
        //Arrange
        _ = nameof(EventListenerNodeImpl.Execute);
        var input = new NodeMsg();
        var eventPayload = new ManagerEventPayload("*", 222);
        input.Add(eventPayload);
        var node = new EventListenerNode { Topics = "*" };

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
    public async Task TriggerEventListenerNodes_RaiseEventFromNodeService_Success(string triggerTopic, string subscribedTopic, bool expectTouched)
    {
        //Arrange
        _ = nameof(EventListenerNodeImpl.Execute);
        _ = nameof(IEventManager.OnTrigger);

        var input = new NodeMsg();
        var eventPayload = new ManagerEventPayload(triggerTopic, 222);
        input.Add(eventPayload);
        var touchedFlag = false;

        var flowNode = new FlowNode();
        var callbackNode = new TestCallBackNode()
        {
            Container = flowNode.Id,
            Callback = (_, _) => touchedFlag = true,
        };
        var node = new EventListenerNode
        {
            Container = flowNode.Id,
            Topics = subscribedTopic,
            Wires = [[callbackNode.Id]]
        };
        _nodeService.Deploy([flowNode, node, callbackNode]);

        //Act
        _eventManager.OnTrigger += Raise.Event<ManagerEventPayloadHandler>(eventPayload);
        await Task.Delay(10);

        //Assert
        touchedFlag.Should().Be(expectTouched);
    }

}
