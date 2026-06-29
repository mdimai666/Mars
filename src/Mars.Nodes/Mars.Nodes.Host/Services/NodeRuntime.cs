using System.Runtime.CompilerServices;
using HandlebarsDotNet;
using Mars.Core.Exceptions;
using Mars.Core.Extensions;
using Mars.Core.Utils;
using Mars.Host.Shared.Hubs;
using Mars.HttpSmartAuthFlow;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.Helpers;
using Mars.Nodes.Host.Shared;
using Mars.Nodes.Host.Shared.ExceptionModule;
using Mars.Nodes.Host.Shared.HttpModule;
using Mars.Nodes.Host.Shared.Models;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] //for NSubstitute

namespace Mars.Nodes.Host.Services;

/// <summary>
/// Nodes runtime process
/// </summary>
internal class NodeRuntime : INodeRuntime
{
    public IServiceProvider ServiceProvider { get; }

    public BroadcastHub BroadcastHub { get; }

    private readonly INodeImplementFactory _nodeImplementFactory;

    public INodeImplementFactory NodeImplementFactory => _nodeImplementFactory;

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

    public CompiledHttpRouteMatcher CompiledHttpRouteMatcher { get; private set; } = default!;

    public event NodeImplDoneEvent OnNodeImplDone = default!;

    public NodesErrorHandlerRegistry ErrorHandlerRegistry { get; private set; } = default!;

    SmartThrottleByKey _broadcastStatusThrottler;

    public NodeRuntime(BroadcastHub hub, INodeImplementFactory nodeImplementFactory, IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        BroadcastHub = hub;
        _nodeImplementFactory = nodeImplementFactory;
        _broadcastStatusThrottler = new(TimeSpan.FromMilliseconds(300));
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

            if (node.IsContainerless) node.Container = string.Empty;

            var hasContainer = node.IsContainerless || !node.Container.IsNullOrEmpty() && dict.ContainsKey(node.Container);

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

    public void AssignNodes(IReadOnlyCollection<Node> nodes, CancellationToken cancellationToken)
    {
        ValidateNodes(nodes);
        InvalidateConfigNodes(Nodes, nodes);

        var diff = DiffList.FindDifferencesBy(_basicNodesDict.Values, nodes, s => s.Id);

        NodeLifecycleOnDelete(diff.ToRemove.Select(s => Nodes[s.Id]).ToArray());

        if (_assignedCount > 0) SaveVarNodeValuesAndClear();
        DisposeNodes(Nodes);
        Nodes.Clear();
        Nodes.EnsureCapacity(nodes.Count);
        HttpRegisterdCatchers.Clear();

        var flowNodes = nodes.OfType<FlowNode>().ToHashSet();

        var flows = flowNodes.Select(node => new FlowNodeImpl(node, null!)).ToDictionary(s => s.Node.Id);
        foreach (var (nodeId, flow) in flows)
        {
            flow.RNS = CreateContextForNode(flow.Node, flow);
            Nodes.Add(nodeId, flow);

            if (!FlowContexts.ContainsKey(nodeId))
                FlowContexts.Add(nodeId, new());
        }

        _configNodesDict = nodes.Where(s => s.IsConfigNode).ToDictionary(s => s.Id, s => (s as ConfigNode)!);

        var nonFlowNodes = nodes.Where(n => !flowNodes.Contains(n)).ToArray();
        foreach (var node in nonFlowNodes)
        {
            var flow = node.IsContainerless ? null : flows[node.Container];
            Nodes.Add(node.Id, _nodeImplementFactory.Create(node, CreateContextForNode(node, flow)));
        }

        _varNodesDict = Nodes.Values.Where(s => s.Node is VarNode).ToDictionary(s => s.Node.Name, s => (s.Node as VarNode)!);
        if (_assignedCount > 0) RestoreVarNodeValues();
        _assignedCount++;
        _basicNodesDict = Nodes.ToDictionary(s => s.Key, s => s.Value.Node);

        CompiledHttpRouteMatcher = new CompiledHttpRouteMatcher(HttpRegisterdCatchers);
        ErrorHandlerRegistry = new NodesErrorHandlerRegistry(_basicNodesDict.Values.OfType<CatchErrorNode>().Where(node => !node.Disabled));

        NodeLifecycleOnAssigned(Nodes, cancellationToken);
    }

    private void InvalidateConfigNodes(Dictionary<string, INodeImplement> oldNodes, IReadOnlyCollection<Node> newNodes)
    {
        var configNodes = newNodes.Where(s => s.IsConfigNode && oldNodes.ContainsKey(s.Id));

        foreach (var node in configNodes)
        {
            var configNodeImpl = oldNodes.GetValueOrDefault(node.Id);
            if (configNodeImpl.Node is ConfigNode configNode)
            {
                if (!configNode.IsEqualAsJsonValues(node))
                {
                    OnInvalidateConfigNode(configNode);
                }
            }
        }
    }

    private void DisposeNodes(Dictionary<string, INodeImplement> oldNodes)
    {
        foreach (var node in oldNodes.Values)
        {
            if (node is IDisposable disposable)
                disposable.Dispose();
            if (node is IAsyncDisposable asyncDisposable)
                asyncDisposable.DisposeAsync();
        }
    }

    private void NodeLifecycleOnAssigned(Dictionary<string, INodeImplement> nodes, CancellationToken cancellationToken)
    {
        foreach (var node in nodes.Values)
        {
            if (node is INodeLifecycleOnAssigned assign)
                assign.OnNodeAssigned(cancellationToken);
        }
    }

    private void NodeLifecycleOnDelete(INodeImplement[] nodes)
    {
        foreach (var node in nodes)
        {
            if (node is INodeLifecycleOnDelete delete)
                delete.OnNodeDelete();
        }
    }

    private void OnInvalidateConfigNode(ConfigNode configNode)
    {
        if (configNode is AuthFlowConfigNode)
        {
            var acm = ServiceProvider.GetRequiredService<AuthClientManager>();
            acm.InvalidateClient(configNode.Id);
        }

    }

    public IRuntimeNodeScope CreateContextForNode(Node node, IFlowNodeImpl? flow)
    {
        if (!node.IsContainerless)
            ArgumentNullException.ThrowIfNull(flow, nameof(flow));
        if (node is FlowNode && flow.Node.Id != node.Id) throw new ArgumentException("For FlowNode flow must be self");
        return new RuntimeNodeScope(node.Id, flow, this, ServiceProvider);
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
        BroadcastHub.DebugMsg(nodeId, msg);
    }

    public virtual void DebugMsg(string nodeId, Exception ex)
    {
        BroadcastHub.DebugMsg(nodeId, ex);
    }

    public virtual void BroadcastStatus(string nodeId, NodeStatus nodeStatus)
    {
        _broadcastStatusThrottler.TryExecute(nodeId, () =>
        {
            BroadcastHub.NodeStatus(nodeId, nodeStatus);
        });
    }

    public void RegisterHttpMiddleware(HttpCatchRegister mw)
    {
        HttpRegisterdCatchers.Add(mw);
    }

    public virtual HttpClient GetHttpClient()
    {
        var httpClient = HttpClientFactory.Create();
        return httpClient;
    }

    public void Done(string nodeId, Guid jobGuid)
        => OnNodeImplDone?.Invoke(nodeId, jobGuid);
}
