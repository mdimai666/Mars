using Mars.Core.Extensions;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Startup;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;

namespace Mars.Nodes.Core.Implements.Managers.Mqtt;

public class MqttManager : IMarsAppLifetimeService, IAsyncDisposable
{
    Dictionary<string, MqttClientInstance> _clientInstances = new();
    private readonly INodeService _nodeService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MqttManager> _logger;
    private Dictionary<string, string[]> _recepientsConfigIdAndNodeIds = new();

    public MqttManager(INodeService nodeService, IServiceScopeFactory scopeFactory, ILogger<MqttManager> logger)
    {
        _nodeService = nodeService;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _nodeService.OnAssignNodes += _nodeService_OnAssignNodes;
    }

    private void _nodeService_OnAssignNodes()
    {
        var configNodes = _nodeService.BaseNodes.Values.Where(node => node is MqttBrokerConfigNode).Select(node => (node as MqttBrokerConfigNode)!).ToArray();
        RefreshConfigs(configNodes);
        UpdateRecepientsDict();
    }

    public MqttClientInstance? GetConfigInstance(string configNodeId) => _clientInstances.GetValueOrDefault(configNodeId);

    public void RefreshConfigs(MqttBrokerConfigNode[] configs)
    {
        _logger.LogInformation("RefreshConfigs");

        var toRemoveIds = _clientInstances.Values.Select(s => s.ConfigNode.Id).Except(configs.Select(s => s.Id)).ToList();
        toRemoveIds.ForEach(configId => _ = _clientInstances[configId].DisposeAsync());

        foreach (var config in configs)
        {
            if (_clientInstances.TryGetValue(config.Id, out var _instance))
            {
                //check param update
                _ = Task.Run(async () =>
                {
                    await _instance.UpdateConfig(config);
                    if (config.ConnectAutomatically)
                        await _instance.UpdateSubscribtions(GetSubscribers(config.Id));
                });
            }
            else
            {
                var instance = new MqttClientInstance(config, this);
                _clientInstances.Add(config.Id, instance);
                if (config.ConnectAutomatically)
                    _ = instance.UpdateSubscribtions(GetSubscribers(config.Id));
            }
        }
    }

    void UpdateRecepientsDict()
    {
        var nodes = _nodeService.BaseNodes.Values.Where(node => !node.Disabled && node is MqttInNode tg && tg.Config.Value != null)
                                            .Select(node => (node as MqttInNode)!).ToArray();

        _recepientsConfigIdAndNodeIds = nodes.GroupBy(s => s.Config.Id).ToDictionary(s => s.Key, s => s.Select(node => node.Id).ToArray());
    }

    MqttNodeSubscribe[] GetSubscribers(string configNodeId)
    {
        var nodes = _nodeService.BaseNodes.Values.Where(node => !node.Disabled && node is MqttInNode tg && tg.Config.Value != null && tg.Config.Id == configNodeId)
                                            .Select(node => (node as MqttInNode)!);

        return nodes.Select(s => new MqttNodeSubscribe(s.Topic, s.QoS)).ToArray();
    }

    internal void OnReciveMessage(MqttClientInstance instance, MqttApplicationMessageReceivedEventArgs message)
    {
        var msg = message.ApplicationMessage;
        _logger.LogTrace($"OnReciveMessage: clientId={message.ClientId}, topic='{msg.Topic}', length={msg.Payload.Length}");

        var nodes = _recepientsConfigIdAndNodeIds.GetValueOrDefault(instance.ConfigNode.Id) ?? [];

        foreach (var _nodeId in nodes)
        {
            var nodeId = _nodeId;
            var payload = new MqttNodeMessagePaylad(message.ApplicationMessage);
            var input = new NodeMsg { Payload = payload.Payload };
            input.Add(payload);
            //input.Add(message.ApplicationMessage);
            _ = _nodeService.Inject(_scopeFactory, nodeId, input);
        }
    }

    internal void OnStatusChange(MqttClientInstance instance, string status)
    {
        _logger.LogTrace($"OnStatusChange: config='{instance.ConfigNode.Name}', status='{status}'");

        var nodes = _recepientsConfigIdAndNodeIds.GetValueOrDefault(instance.ConfigNode.Id) ?? [];

        foreach (var nodeId in nodes)
        {
            _nodeService.BroadcastStatus(nodeId, new NodeStatus { Text = status });
        }
    }

    [StartupOrder(11)]
    public Task OnStartupAsync()
    {
        _nodeService_OnAssignNodes();
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await Task.WhenAll(_clientInstances.Values.Select(s => s.DisposeAsync().AsTask()).ToArray());
    }

}
