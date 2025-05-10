using Mars.Core.Exceptions;
using Mars.Host.Shared.Hubs;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements;
using Mars.Nodes.Core.Implements.Models;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Nodes;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mars.Nodes.Host.Services;

/// <summary>
/// Node-red runtime context
/// </summary>
internal class RED
{
    public IServiceProvider ServiceProvider { get; }

    public IHubContext<ChatHub> Hub;// => ChatHub.instance;
    public List<HttpCatchRegister> HttpRegisterdCatchers { get; set; } = new();
    public VariablesContextDictionary GlobalContext { get; } = new();

    public Dictionary<string, VariablesContextDictionary> FlowContexts { get; } = new();

    public Dictionary<string, INodeImplement> Nodes { get; set; } = new Dictionary<string, INodeImplement>();

    private Dictionary<string, VarNode> _varNodesDict = new();
    public IReadOnlyDictionary<string, VarNode> VarNodesDict => _varNodesDict;

    private Dictionary<string, ConfigNode> _configNodesDict = new();
    public IReadOnlyDictionary<string, ConfigNode> ConfigNodesDict => _configNodesDict;

    private int _assignedCount = 0;

    public RED(IHubContext<ChatHub> hub, IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        Hub = hub;
    }

    void ValidateNodes(IEnumerable<Node> nodes)
    {
        var dict = nodes.ToDictionary(s => s.Id);
        bool allNodesHasContainer = nodes.All(node => node.Type == typeof(FlowNode).FullName || !string.IsNullOrEmpty(node.Container) && dict.ContainsKey(node.Container));
        if (!allNodesHasContainer)
        {
            throw new MarsValidationException(new Dictionary<string, string[]>() { ["nodes"] = ["some node has not valid 'Container'"] });
        }
    }

    public void AssignNodes(List<Node> nodes)
    {
        ValidateNodes(nodes);

        if (_assignedCount > 0) SaveVarNodeValuesAndClear();
        Nodes.Clear();
        Nodes.EnsureCapacity(nodes.Count());
        HttpRegisterdCatchers.Clear();

        var flowNodes = nodes.Where(node => node is FlowNode).Select(node => (FlowNode)node);
        var flows = flowNodes.Select(node => new FlowNodeImpl(node, null!)).ToDictionary(s => s.Node.Id);
        foreach (var (key, flow) in flows)
        {
            flow.RED = CreateContextForNode(flow.Node, flow);
            Nodes.Add(key, flow);
        }

        _configNodesDict = nodes.Where(s => s.IsConfigNode).ToDictionary(s => s.Id, s => (s as ConfigNode)!);

        foreach (var node in nodes.Except(flowNodes))
        {
            var flow = node is FlowNode ? flows[node.Id] : flows[node.Container];
            Nodes.Add(node.Id, NodeImplementFabirc.Create(node, CreateContextForNode(node, flow)));
        }

        _varNodesDict = Nodes.Values.Where(s => s.Node is VarNode).ToDictionary(s => s.Node.Name, s => (s.Node as VarNode)!);
        if (_assignedCount > 0) RestoreVarNodeValues();
        _assignedCount++;
    }

    public RED_Context CreateContextForNode(Node node, FlowNodeImpl flow)
    {
        ArgumentNullException.ThrowIfNull(flow, nameof(flow));
        if (node is FlowNode && flow.Node.Id != node.Id) throw new ArgumentException("For FlowNode flow must be self");
        //var flow = Nodes[node.Container] as FlowNodeImpl;
        return new RED_Context(node.Id, flow!, ServiceProvider);
    }

    public IEnumerable<HttpCatchRegister> GetHttpCatchRegistersForMethod(string method)
    {
        return HttpRegisterdCatchers.Where(s => s.Method == "*" || s.Method == method);
    }

    IDictionary<string, object?>? _temporarilyStoredNodeValues;

    private void SaveVarNodeValuesAndClear()
    {
        _temporarilyStoredNodeValues = _varNodesDict.Values.ToDictionary(s => s.Id, s => s.Value);
        foreach (var node in _varNodesDict.Values)
        {
            node.Value = null;
        }
    }

    private void RestoreVarNodeValues()
    {
        foreach (var node in _varNodesDict.Values)
        {
            if (_temporarilyStoredNodeValues.TryGetValue(node.Id, out var storedvalue))
            {
                node.Value = storedvalue;
            }
        }
        _temporarilyStoredNodeValues = null;
    }

    public VarNodeVaribleDto? GetVarNodeVarible(string varName)
        => _varNodesDict.GetValueOrDefault(varName)?.GetDto();

    public void SetVarNodeVarible(string varName, object? value)
    {
        if (_varNodesDict.TryGetValue(varName, out var varNode))
        {
            varNode.SetValue(value);
        }
        else
        {
            throw new ArgumentNullException($"VarNode Name='{varName}' not found");
        }
    }

    public virtual void DebugMsg(DebugMessage msg)
    {
        Hub.Clients.All.SendAsync("DebugMsg", "", msg);
    }

    public virtual void DebugMsg(Exception ex)
    {
        Hub.Clients.All.SendAsync("DebugMsg", "", new DebugMessage
        {
            message = ex.Message,
            Level = Mars.Core.Models.MessageIntent.Error,
        });
    }

    public virtual void Status(NodeStatus nodeStatus)
    {
        Hub.Clients.All.SendAsync("NodeStatus", "", nodeStatus);
    }

    public void RegisterHttpMiddleware(HttpCatchRegister mw)
    {
        HttpRegisterdCatchers.Add(mw);
    }

    public HttpClient GetHttpClient()
    {
        HttpClient httpClient = HttpClientFactory.Create();
        return httpClient;
    }

}

internal class RED_Context : IRED
{
    public string NodeId { get; set; }
    public FlowNodeImpl Flow { get; }
    public ILogger<IRED> Logger { get; }
    public List<HttpCatchRegister> HttpRegisterdCatchers => RED.HttpRegisterdCatchers;
    public IServiceProvider ServiceProvider { get; }

    public VariablesContextDictionary GlobalContext => RED.GlobalContext;
    public VariablesContextDictionary FlowContext => RED.FlowContexts[Flow.Node.Id];

    RED RED;

    public RED_Context(string nodeId, FlowNodeImpl flow, IServiceProvider serviceProvider)
    {
        NodeId = nodeId;
        Flow = flow;
        ServiceProvider = serviceProvider;

        Logger = MarsLogger.GetStaticLogger<IRED>();
        RED = serviceProvider.GetRequiredService<RED>();

        if (!RED.FlowContexts.ContainsKey(Flow.Node.Id)) RED.FlowContexts.Add(Flow.Node.Id, new());
    }

    public void DebugMsg(DebugMessage msg)
        => RED.Hub.Clients.All.SendAsync("DebugMsg", NodeId, msg);

    public void DebugMsg(Exception ex)
    {
        RED.Hub.Clients.All.SendAsync("DebugMsg", NodeId, new DebugMessage
        {
            message = ex.Message,
            Level = Mars.Core.Models.MessageIntent.Error,
        });
    }

    public void Status(NodeStatus nodeStatus)
        => RED.Hub.Clients.All.SendAsync("NodeStatus", NodeId, nodeStatus);

    public void RegisterHttpMiddleware(HttpCatchRegister mw)
        => RED.RegisterHttpMiddleware(mw);

    public IEnumerable<HttpCatchRegister> GetHttpCatchRegistersForMethod(string method)
        => RED.GetHttpCatchRegistersForMethod(method);

    public HttpClient GetHttpClient()
        => RED.GetHttpClient();

    public VarNodeVaribleDto? GetVarNodeVarible(string varName)
        => RED.GetVarNodeVarible(varName);

    public void SetVarNodeVarible(string varName, object? value)
        => RED.SetVarNodeVarible(varName, value);

    public IReadOnlyDictionary<string, VarNode> VarNodesDict => RED.VarNodesDict;
    public IReadOnlyDictionary<string, ConfigNode> ConfigNodesDict => RED.ConfigNodesDict;

    public InputConfig<TConfigNode> GetConfig<TConfigNode>(string id) where TConfigNode : ConfigNode
        => new() { Id = id, Value = RED.ConfigNodesDict.GetValueOrDefault(id) as TConfigNode };

    public InputConfig<TConfigNode> GetConfig<TConfigNode>(InputConfig<TConfigNode> config) where TConfigNode : ConfigNode
        => new() { Id = config.Id, Value = RED.ConfigNodesDict.GetValueOrDefault(config.Id) as TConfigNode };
}



