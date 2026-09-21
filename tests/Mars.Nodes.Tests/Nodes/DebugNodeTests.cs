using AutoFixture;
using FluentAssertions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements.Nodes.Common;
using Mars.Nodes.Tests.Services;
using MQTTnet;
using MQTTnet.Packets;
using NSubstitute;

namespace Mars.Nodes.Tests.Nodes;

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
        await Task.Delay(100);
        Runtime.Received(1).DebugMsg(node.Id, Arg.Is<DebugMessage>(
            msg => !msg.Message.Contains("error", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrEmpty(msg.Json)
            ));
    }

    [Fact]
    public async Task Execute_LegacyAtPath_ReadsPayload()
    {
        //Arrange
        _ = nameof(DebugNodeImpl.Execute);
        var node = new DebugNode { PropertyPath = "@msg.Payload" };
        var input = new NodeMsg { Payload = new { Mark = "legacy" } };

        //Act
        await ExecuteNode(node, input);

        //Assert
        await Task.Delay(100);
        Runtime.Received(1).DebugMsg(node.Id, Arg.Is<DebugMessage>(msg => msg.Json != null && msg.Json.Contains("legacy")));
    }

    [Fact]
    public async Task Execute_MsgContextKeyPath_SerializesValue()
    {
        //Arrange
        _ = nameof(DebugNodeImpl.Execute);
        var node = new DebugNode { PropertyPath = "msg.User" };
        var input = new NodeMsg { Payload = "plain" };
        input.Set("User", new { Mark = "ctxmark" });

        //Act
        await ExecuteNode(node, input);

        //Assert
        await Task.Delay(100);
        Runtime.Received(1).DebugMsg(node.Id, Arg.Is<DebugMessage>(msg => msg.Json != null && msg.Json.Contains("ctxmark")));
    }

    [Fact]
    public async Task Execute_GlobalContextPath_SerializesValue()
    {
        //Arrange
        _ = nameof(DebugNodeImpl.Execute);
        Runtime.GlobalContext.SetValue("g1", new { Mark = "gmark" });
        var node = new DebugNode { PropertyPath = "GlobalContext.g1" };

        //Act
        await ExecuteNode(node, new NodeMsg { Payload = "plain" });

        //Assert
        await Task.Delay(100);
        Runtime.Received(1).DebugMsg(node.Id, Arg.Is<DebugMessage>(msg => msg.Json != null && msg.Json.Contains("gmark")));
    }
}
