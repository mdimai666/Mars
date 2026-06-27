using Mars.Nodes.Core.Implements.Managers.Mqtt;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.Shared;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;

namespace Mars.Nodes.Core.Implements.Nodes;

public class MqttOutNodeImpl : INodeImplement<MqttOutNode>
{
    private MqttManager _mqttManager = default!;

    public MqttOutNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public MqttOutNodeImpl(MqttOutNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;

        Node.Config = RNS.GetConfig(node.Config);
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        _mqttManager ??= RNS.ServiceProvider.GetRequiredService<MqttManager>();

        if (Node.Config.Value == null) throw new Exception("not configured");

        var instance = _mqttManager.GetConfigInstance(Node.Config.Id);
        var mqttClient = await instance.GetConnectedClient();

        //TODO: add support bytes

        var isJson = false;
        string payload;
        if (input.Payload is null) payload = "";
        else if (input.Payload is string str) payload = str;
        else if (input.Payload.GetType().IsPrimitive) payload = input.Payload.ToString()!;
        else
        {
            isJson = true;
            payload = JsonNodeImpl.ToJsonString(input.Payload);
        }

        var applicationMessage = new MqttApplicationMessageBuilder()
            .WithTopic(Node.Topic)
            .WithPayload(payload)
            .WithContentType(isJson ? "application/json" : "text/plain")
            .WithQualityOfServiceLevel(MqttClientInstance.ConvertQoS(Node.QoS))
            .Build();

        await mqttClient.PublishAsync(applicationMessage, parameters.CancellationToken);
    }
}
