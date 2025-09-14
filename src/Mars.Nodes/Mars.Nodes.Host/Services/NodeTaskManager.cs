using Mars.Host.Shared.Hubs;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Exceptions;
using Mars.Nodes.Core.Implements;
using Mars.Nodes.Core.Implements.Nodes;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Mars.Nodes.Host.Services;

internal class NodeTaskManager : IDisposable
{
    protected readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<ChatHub> _hub;
    protected readonly IReadOnlyDictionary<string, INodeImplement> _nodes;
    protected readonly RED _RED;
    private readonly ILogger<NodeTaskManager> _logger;
    int executedCount = 0;
    private readonly int maxExecuteCount = 1_000;

    public NodeTaskManager(IServiceProvider serviceProvider,
        IHubContext<ChatHub> hub,
        IReadOnlyDictionary<string, INodeImplement> nodes,
        RED RED,
        ILogger<NodeTaskManager> logger)
    {
        _nodes = nodes;
        _RED = RED;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _hub = hub;
    }

    public void Run(string nodeId, NodeMsg? msg = null)
    {
        _logger.LogTrace("Run");
        var node = _nodes[nodeId];

        node.RED = CreateContextForNode(nodeId);

        _ = node.Execute(msg ?? new NodeMsg(), (e, output) => { CallbackNext(node.Id, e, output); }, new());
    }

    void CallbackNext(string completedNodeId, NodeMsg result, int output)
    {
        executedCount++;
        if (executedCount > maxExecuteCount)
        {
            _logger.LogError($"MAX_EXECUTE_COUNT={maxExecuteCount}");
            throw new Exception($"MAX_EXECUTE_COUNT={maxExecuteCount}");
        }

        var nextNodes = GetNextWires(completedNodeId, output);

        foreach (var _wire in nextNodes)
        {
            var node = _nodes[_wire.NodeId];
            var portIndex = _wire.PortIndex;
            if (node.Node.Disabled) continue;

            node.RED = CreateContextForNode(node.Id);
            ExecuteNode(result, node, new(InputPort: portIndex));
        }

    }

    private async void ExecuteNode(NodeMsg input, INodeImplement node, ExecutionParameters parameters)
    {
        try
        {
            await node.Execute(input,
                (e, _output) =>
                {
                    _logger.LogTrace($"call next wire = {node.Node.DisplayName}({node.Node.Type}/{node.Id})");
                    CallbackNext(node.Id, e, _output);
                },
                parameters);
        }
        catch (NodeExecuteException ex)
        {
            _logger.LogError(ex, "node execute exception");
            _RED.DebugMsg(node.Id, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "node execute exception");
            _RED.DebugMsg(node.Id, ex);
        }
    }

    public RED_Context CreateContextForNode(string nodeId)
    {
        var node = _nodes[nodeId];
        var flow = node is FlowNodeImpl ? node : _nodes[node.Node.Container];
        return new RED_Context(nodeId, (FlowNodeImpl)flow, _serviceProvider);
    }

    IEnumerable<NodeWire> GetNextWires(string nodeId, int outputIndex)
    {
        var node = _nodes[nodeId];

        var outsWires = node.Node.Wires.ElementAtOrDefault(outputIndex);

        if (outsWires == null) return Enumerable.Empty<NodeWire>();

        return outsWires;
    }

    public void Dispose()
    {
        _logger.LogTrace($"Dispose; executedCount={executedCount}");
    }
}
