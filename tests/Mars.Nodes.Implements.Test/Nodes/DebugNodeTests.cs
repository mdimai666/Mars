using AutoFixture;
using FluentAssertions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Implements.Test.Services;
using MQTTnet;
using MQTTnet.Packets;
using NSubstitute;

namespace Mars.Nodes.Implements.Test.Nodes;

public class DebugNodeTests : NodeServiceUnitTestBase
{

    [Fact]
    public async Task Execute_PayloadCompleteSerialize_MustFireNonErrorMessage()
    {
        //Arrange
        _ = nameof(DebugNodeImpl.Execute);
        var appMsg = _fixture.Create<MqttApplicationMessage>();
        var packet = _fixture.Create<MqttPublishPacket>();
        var recivedMessage = new MqttApplicationMessageReceivedEventArgs("client_id", appMsg, packet, null);
        var input = new NodeMsg() { Payload = recivedMessage };
        input.Add(recivedMessage);

        var node = new DebugNode { CompleteInputMessage = true };

        //Act
        var action = () => ExecuteNode(node, input);

        //Assert
        await action.Should().NotThrowAsync();
        RED.Received().DebugMsg(node.Id, Arg.Is<DebugMessage>(
            msg => !msg.Message.Contains("error", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrEmpty(msg.Json)
            ));
    }
}
