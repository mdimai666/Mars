using System.Runtime.CompilerServices;
using Mars.Core.Exceptions;
using Mars.Host.Shared.Hubs;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Fields;
using Mars.Nodes.Core.Implements;
using Mars.Nodes.Core.Implements.Models;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Nodes;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] //for NSubstitute

namespace Mars.Nodes.Host.Services;

/// <summary>
/// Node-red runtime context
/// </summary>
internal class RED
{
    public IServiceProvider ServiceProvider { get; }

    public IHubContext<ChatHub> Hub;
    private readonly NodeImplementFactory _nodeImplementFactory;

    public List<HttpCatchRegister> HttpRegisterdCatchers { get; set; } = [];
    public VariablesContextDictionary GlobalContext { get; } = new();

    public Dictionary<string, VariablesContextDictionary> FlowContexts { get; } = [];

    public Dictionary<string, INodeImplement> Nodes { get; set; } = [];

    private Dictionary<string, VarNode> _varNodesDict = [];
    public IReadOnlyDictionary<string, VarNode> VarNodesDict => _varNodesDict;

    private Dictionary<string, ConfigNode> _configNodesDict = [];
    public IReadOnlyDictionary<string, ConfigNode> ConfigNodesDict => _configNodesDict;

    private Dictionary<string, Node> _basicNodesDict = [];
    public IReadOnlyDictionary<string, Node> BasicNodesDict => _basicNodesDict;

    private int _assignedCount = 0;

    public event NodeImplDoneEvent OnNodeImplDone = default!;

    public RED(IHubContext<ChatHub> hub, NodeImplementFactory nodeImplementFactory, IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        Hub = hub;
        _nodeImplementFactory = nodeImplementFactory;
    }

    void ValidateNodes(IEnumerable<Node> nodes)
    {
        var dict = nodes.ToDictionary(s => s.Id);
        var errors = new Dictionary<string, string[]>();

        foreach (var node in nodes)
        {
            var nodeErrors = new List<string>();

            if (node.GetType() == typeof(Node))
                nodeErrors.Add("All nodes must be of derived types.");

            var hasContainer = node.Type == typeof(FlowNode).FullName
                                || node.Type == typeof(UnknownNode).FullName
                                || !string.IsNullOrEmpty(node.Container) && dict.ContainsKey(node.Container);
            if (!hasContainer)
            {
                nodeErrors.Add($"node(id='{node.Id}', name='{node.Label}') has no container");
            }

            if (nodeErrors.Any())
            {
                errors.Add(node.Id, nodeErrors.ToArray());
            }
        }

        if (errors.Any())
        {
            throw new MarsValidationException(errors);
        }
    }

    public void AssignNodes(IReadOnlyCollection<Node> nodes)
    {
        ValidateNodes(nodes);

        //if (nodes.Count == 0)
        //{
        //    nodes.Add(new FlowNode
        //    {
        //        Name = "Flow 0",
        //    });
        //}

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
            Nodes.Add(node.Id, _nodeImplementFactory.Create(node, CreateContextForNode(node, flow)));
        }

        _varNodesDict = Nodes.Values.Where(s => s.Node is VarNode).ToDictionary(s => s.Node.Name, s => (s.Node as VarNode)!);
        if (_assignedCount > 0) RestoreVarNodeValues();
        _assignedCount++;
        _basicNodesDict = Nodes.ToDictionary(s => s.Key, s => s.Value.Node);
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

    public virtual void DebugMsg(string nodeId, DebugMessage msg)
    {
        Hub.Clients.All.SendAsync("DebugMsg", nodeId, msg);
    }

    public virtual void DebugMsg(string nodeId, Exception ex)
    {
        Hub.Clients.All.SendAsync("DebugMsg", nodeId, new DebugMessage
        {
            NodeId = nodeId,
            Message = ex.Message,
            Level = Mars.Core.Models.MessageIntent.Error,
        });
    }

    public virtual void BroadcastStatus(string nodeId, NodeStatus nodeStatus)
    {
        Hub.Clients.All.SendAsync("NodeStatus", nodeId, nodeStatus);
    }

    public void RegisterHttpMiddleware(HttpCatchRegister mw)
    {
        HttpRegisterdCatchers.Add(mw);
    }

    public HttpClient GetHttpClient()
    {
        var httpClient = HttpClientFactory.Create();
        return httpClient;
    }

    public void Done(string nodeId, Guid jobGuid)
        => OnNodeImplDone?.Invoke(nodeId, jobGuid);
}

internal delegate void NodeImplDoneEvent(string nodeId, Guid jobGuid);

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
        => RED.DebugMsg(NodeId, msg);

    public void DebugMsg(Exception ex)
        => RED.DebugMsg(NodeId, ex);

    public void Status(NodeStatus nodeStatus)
        => RED.BroadcastStatus(NodeId, nodeStatus);

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
        => new() { Id = id, Value = RED.ConfigNodesDict.GetValueOrDefault(id ?? "") as TConfigNode };

    public InputConfig<TConfigNode> GetConfig<TConfigNode>(InputConfig<TConfigNode> config) where TConfigNode : ConfigNode
        => new() { Id = config.Id, Value = RED.ConfigNodesDict.GetValueOrDefault(config.Id ?? "") as TConfigNode };

    public void Done(ExecutionParameters parameters)
        => RED.Done(NodeId, parameters.JobGuid);
}
