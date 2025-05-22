using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using Mars.Nodes.Core.Implements.JsonConverters;
using MQTTnet;
using MQTTnet.Packets;

namespace Mars.Nodes.Implements.Test.JsonConverters;

public class IgnoreReadOnlySpanPropertiesResolverTests
{
    private readonly IFixture _fixture;

    public IgnoreReadOnlySpanPropertiesResolverTests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void Serialize_SerializeSpanBytes_ShouldSuccess()
    {
        //Arrange
        _ = nameof(MqttApplicationMessageReceivedEventArgs);
        var msg = _fixture.Create<MqttApplicationMessage>();
        var packet = _fixture.Create<MqttPublishPacket>();
        var recivedMessage = new MqttApplicationMessageReceivedEventArgs("client_id", msg, packet, null);

        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = new IgnoreReadOnlySpanPropertiesResolver(),
            WriteIndented = true,
        };

        //Act
        //Assert
        string json = "";
        var action = () => json = JsonSerializer.Serialize(recivedMessage.ApplicationMessage, options);
        action.Should().NotThrow();

    }
}
