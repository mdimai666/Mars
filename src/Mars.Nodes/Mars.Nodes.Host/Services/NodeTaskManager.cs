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
    private readonly int maxExecuteCount = 1_00;

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
        //using var scope = serviceProvider.CreateScope();
        //var _red = scope.ServiceProvider.GetService<RED_withNode>()!;
        //_red.NodeId = node.Id;
        //node.RED = _red;


        _ = node.Execute(msg ?? new NodeMsg(), (e, output) => { CallbackNext(node.Id, e, output); }, null!);
    }

    void CallbackNext(string completedNodeId, NodeMsg result, int output)
    {
        executedCount++;
        if (executedCount > maxExecuteCount)
        {
            _logger.LogError($"MAX_EXECUTE_COUNT={maxExecuteCount}");
            throw new Exception($"MAX_EXECUTE_COUNT={maxExecuteCount}");
        }

        var nextNodes = GetNextWires(completedNodeId, output).Where(nodeImpl => !nodeImpl.Node.Disabled);

        foreach (var _node in nextNodes)
        {
            var node = _node;
            node.RED = CreateContextForNode(node.Id);
            ExecuteNode(result, node);
        }

    }

    private async void ExecuteNode(NodeMsg input, INodeImplement node)
    {
        try
        {
            await node.Execute(input,
                (e, _output) =>
                {
                    _logger.LogTrace($"call next wire = {node.Node.DisplayName}({node.Node.Type}/{node.Id})");
#if DEBUG
                    Console.WriteLine($"==>{node.Node.DisplayName} ({node.Node.Type}) + {e.Payload}");
#endif
                    CallbackNext(node.Id, e, _output);
                }, null!);
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

    IEnumerable<INodeImplement> GetNextWires(string nodeId, int outputIndex)
    {
        var node = _nodes[nodeId];
        //var flow = (nodes[node.Node.Container] as FlowNodeImpl)!;

        var outsIds = node.Node.Wires.ElementAtOrDefault(outputIndex);

        if (outsIds == null) return Enumerable.Empty<INodeImplement>();

        IEnumerable<INodeImplement> nextNodes = _nodes.Values.Where(s => outsIds.Contains(s.Id));

        return nextNodes;

    }

    public void Dispose()
    {
        _logger.LogTrace($"Dispose; executedCount={executedCount}");
        Console.WriteLine("NodeTaskManager::Dispose");
    }
}
