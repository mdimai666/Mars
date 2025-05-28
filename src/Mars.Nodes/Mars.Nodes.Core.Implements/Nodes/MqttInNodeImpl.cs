using System.Buffers;
using System.Text;
using Mars.Nodes.Core.Implements.Managers.Mqtt;
using Mars.Nodes.Core.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;

namespace Mars.Nodes.Core.Implements.Nodes;

public class MqttInNodeImpl : INodeImplement<MqttInNode>, INodeImplement
{
    public MqttInNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;
    private readonly ILogger<MqttInNodeImpl> _logger;


    public MqttInNodeImpl(MqttInNode node, IRED red)
    {
        Node = node;
        RED = red;

        Node.Config = RED.GetConfig(node.Config);
        _logger = RED.ServiceProvider.GetRequiredService<ILogger<MqttInNodeImpl>>();

    }

    /// <summary>
    /// <see cref="MqttManager.OnReciveMessage(MqttClientInstance, MqttApplicationMessageReceivedEventArgs)"/>
    /// </summary>
    /// <param name="input"></param>
    /// <param name="callback"></param>
    /// <param name="Error"></param>
    /// <returns></returns>
    public Task Execute(NodeMsg input, ExecuteAction callback)
    {
        _logger.LogTrace("Execute");

        var mqttMessage = input.Get<MqttNodeMessagePaylad>();

        input.Payload = mqttMessage.Payload;

        callback(input);
        return Task.CompletedTask;
    }
}

public class MqttNodeMessagePaylad
{
    public string? ContentType { get; set; }
    public bool Dup { get; set; }
    public uint MessageExpiryInterval { get; set; }
    public NodeMqttQualityOfServiceLevel QoS { get; set; }
    public string? ResponseTopic { get; set; }
    public bool Retain { get; set; }
    public string Topic { get; set; } = "";

    public string? Payload { get; set; }

    byte[] _payloadBytes = [];

    public MqttNodeMessagePaylad()
    {

    }

    public MqttNodeMessagePaylad(MqttApplicationMessage message)
    {
        ContentType = message.ContentType;
        Dup = message.Dup;
        MessageExpiryInterval = message.MessageExpiryInterval;
        QoS = MqttClientInstance.ConvertQoS(message.QualityOfServiceLevel);
        ResponseTopic = message.ResponseTopic;
        Retain = message.Retain;
        Topic = message.Topic;

        _payloadBytes = message.Payload.ToArray();

        Payload = System.Text.Encoding.UTF8.GetString(message.Payload);
    }

    public byte[] GetBytes() => _payloadBytes;
}
