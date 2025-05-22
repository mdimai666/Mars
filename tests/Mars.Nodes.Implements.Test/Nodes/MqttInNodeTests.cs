using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using Mars.Nodes.Core.Implements.Nodes;
using MQTTnet;
using MQTTnet.Packets;

namespace Mars.Nodes.Implements.Test.Nodes;

public class MqttInNodeTests
{
    private readonly IFixture _fixture;

    public MqttInNodeTests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void MqttIncomingMessagePayload_ToJson_ShouldSuccess()
    {
        //Arrange
        _ = nameof(MqttApplicationMessageReceivedEventArgs);
        var msg = _fixture.Create<MqttApplicationMessage>();
        var packet = _fixture.Create<MqttPublishPacket>();
        var recivedMessage = new MqttApplicationMessageReceivedEventArgs("client_id", msg, packet, null);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        var paylad = new MqttNodeMessagePaylad(recivedMessage.ApplicationMessage);

        //Act
        var action = () => JsonSerializer.Serialize(paylad, options);

        //Assert
        action.Should().NotThrow();
    }
}
