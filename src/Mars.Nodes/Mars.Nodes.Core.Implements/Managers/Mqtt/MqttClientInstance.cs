using Mars.Core.Extensions;
using Mars.Core.Utils;
using Mars.Nodes.Core.Nodes;
using MQTTnet;
using MQTTnet.Protocol;

namespace Mars.Nodes.Core.Implements.Managers.Mqtt;

public class MqttClientInstance : IAsyncDisposable
{
    private readonly MqttManager _mqttManager;
    public DateTime CreatedAt { get; } = new();
    public MqttBrokerConfigNode ConfigNode { get; private set; }
    public IMqttClient? Client { get; private set; }

    public HashSet<MqttNodeSubscribe> Subscribes = [];

    public MqttClientInstance(MqttBrokerConfigNode configNode, MqttManager mqttManager)
    {
        ConfigNode = configNode;
        _mqttManager = mqttManager;

        var mqttFactory = new MqttClientFactory();
        var mqttClient = mqttFactory.CreateMqttClient();
        Client = mqttClient;

        Client.ApplicationMessageReceivedAsync += Client_ApplicationMessageReceivedAsync;
        Client.ConnectedAsync += Client_ConnectedAsync;
        Client.DisconnectedAsync += Client_DisconnectedAsync;
        Client.ConnectingAsync += Client_ConnectingAsync;
    }

    public async Task UpdateConfig(MqttBrokerConfigNode newConfig)
    {
        var isEqual = ConfigNode.IsEqualRootPropertyValues(newConfig);
        if (isEqual) return;
        ConfigNode = newConfig;

        if (Client.IsConnected) await Client.DisconnectAsync();
        _ = Client.ConnectAsync(Configuration(ConfigNode));
    }

    public async Task Subscribe(string topic, MqttQualityOfServiceLevel qos = MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce)
    {
        var client = await GetConnectedClient();

        await client.SubscribeAsync(topic, qos, CancellationToken.None);
    }

    public async Task Unsubscribe()
    {
        var client = await GetConnectedClient();

        var mqttFactory = new MqttClientFactory();
        var mqttSubscribeOptions = mqttFactory.CreateUnsubscribeOptionsBuilder().Build();

        await client.UnsubscribeAsync(mqttSubscribeOptions, CancellationToken.None);
    }

    public async Task UpdateSubscribtions(IEnumerable<MqttNodeSubscribe> newSubscribes)
    {
        newSubscribes = newSubscribes.Distinct().ToArray();

        var result = DiffList.FindDifferences(Subscribes, newSubscribes.ToHashSet()); //тут тип record сравнивает по значениям
        if (!result.HasChanges && Client.IsConnected) return;
        Subscribes = newSubscribes.ToHashSet();

        if (!Client.IsConnected) await Client.ConnectAsync(Configuration(ConfigNode));
        if (!Client.IsConnected) return;

        foreach (var sub in result.ToRemove)
        {
            await Client.UnsubscribeAsync(sub.Topic);
        }
        foreach (var sub in result.ToAdd)
        {
            await Client.SubscribeAsync(sub.Topic, ConvertQoS(sub.qos));
        }

    }

    private Task Client_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        _mqttManager.OnReciveMessage(this, arg);
        return Task.CompletedTask;
    }

    private Task Client_ConnectingAsync(MqttClientConnectingEventArgs arg)
    {
        _mqttManager.OnStatusChange(this, "connecting...");
        return Task.CompletedTask;
    }

    private Task Client_DisconnectedAsync(MqttClientDisconnectedEventArgs arg)
    {
        _mqttManager.OnStatusChange(this, "disconnected");
        return Task.CompletedTask;
    }

    private Task Client_ConnectedAsync(MqttClientConnectedEventArgs arg)
    {
        _mqttManager.OnStatusChange(this, "connected");
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (Client is null) return;

        try { await Unsubscribe(); } catch { }

        Client.ApplicationMessageReceivedAsync -= Client_ApplicationMessageReceivedAsync;
        Client.ConnectedAsync -= Client_ConnectedAsync;
        Client.DisconnectedAsync -= Client_DisconnectedAsync;
        Client.ConnectingAsync -= Client_ConnectingAsync;
        Client.Dispose();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<IMqttClient?> GetConnectedClient()
    {
        if (Client is null) return null;
        if (Client.IsConnected) return Client;
        var result = await Client.ConnectAsync(Configuration(ConfigNode));
        if (result.ResultCode != MqttClientConnectResultCode.Success) throw new Exception(result.ResultCode.ToString());
        return Client;
    }

    public static MqttClientOptions Configuration(MqttBrokerConfigNode configNode)
    {
        var mqttClientOptionsBuilder = new MqttClientOptionsBuilder()
                    .WithTcpServer(configNode.Host, configNode.Port)
                    .WithProtocolVersion(ConvertVersion(configNode.ProtocolVersion))
                    .WithCleanStart(configNode.UseCleanStart)
                    .WithKeepAlivePeriod(TimeSpan.FromSeconds(configNode.KeepAlivePeriodInSeconds))
                    ;
        if (configNode.Username.AsNullIfEmptyOrWhiteSpace() != null || configNode.Password.AsNullIfEmptyOrWhiteSpace() != null)
        {
            mqttClientOptionsBuilder.WithCredentials(configNode.Username, configNode.Password);
        }

        var mqttClientOptions = mqttClientOptionsBuilder.Build();
        return mqttClientOptions;
    }

    public static MQTTnet.Formatter.MqttProtocolVersion ConvertVersion(MqttBrokerConfigNode.NodeMqttProtocolVersion version)
        => version switch
        {
            MqttBrokerConfigNode.NodeMqttProtocolVersion.V310 => MQTTnet.Formatter.MqttProtocolVersion.V310,
            MqttBrokerConfigNode.NodeMqttProtocolVersion.V311 => MQTTnet.Formatter.MqttProtocolVersion.V311,
            MqttBrokerConfigNode.NodeMqttProtocolVersion.V500 => MQTTnet.Formatter.MqttProtocolVersion.V500,
            _ => throw new NotImplementedException($"version '{version}' not implement")
        };

    public static MQTTnet.Protocol.MqttQualityOfServiceLevel ConvertQoS(NodeMqttQualityOfServiceLevel qos)
        => qos switch
        {
            NodeMqttQualityOfServiceLevel.AtMostOnce => MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce,
            NodeMqttQualityOfServiceLevel.AtLeastOnce => MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce,
            NodeMqttQualityOfServiceLevel.ExactlyOnce => MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce,
            _ => throw new NotImplementedException($"qos '{qos}' not implement")
        };

    public static NodeMqttQualityOfServiceLevel ConvertQoS(MQTTnet.Protocol.MqttQualityOfServiceLevel qos)
        => qos switch
        {
            MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce => NodeMqttQualityOfServiceLevel.AtMostOnce,
            MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce => NodeMqttQualityOfServiceLevel.AtLeastOnce,
            MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce => NodeMqttQualityOfServiceLevel.ExactlyOnce,
            _ => throw new NotImplementedException($"qos '{qos}' not implement")
        };

}

public record MqttNodeSubscribe(string Topic, NodeMqttQualityOfServiceLevel qos);
