using Mars.Nodes.Core;
using Mars.Nodes.Core.Exceptions;
using Mars.Nodes.Core.Implements;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Utils;
using Mars.Nodes.Host.Services;
using Microsoft.Extensions.Logging;

namespace Mars.Nodes.Host.NodeTasks;

internal class NodeTaskJob : IAsyncDisposable
{
    public Guid TaskId { get; } = Guid.NewGuid();
    protected readonly IServiceProvider _serviceProvider;
    protected readonly IReadOnlyDictionary<string, INodeImplement> _nodes;
    protected Dictionary<string, NodeJob> _jobs = [];
    protected readonly RED _RED;
    private readonly ILogger<NodeTaskJob> _logger;
    int executedCount;
    private readonly int maxExecuteCount = 10_000;
    bool isAllDone => _jobs.Values.All(s => s.IsDone);

    public string InjectNodeId { get; }
    public string FlowNodeId { get; }

    public event Action? OnComplete;
    public int ExecuteCount => executedCount;
    public int NodesChainCount { get; }
    public IReadOnlyDictionary<string, NodeJob> Jobs => _jobs;
    public bool IsDone => isAllDone;
    public bool IsTerminated { get; private set; }
    public int ErrorCount => _jobs.Values.Sum(s => s.Executions.Count(x => x.Result == NodeJobExecutionResult.Fail));
    readonly CancellationTokenSource _cancellationTokenSource = new();

    internal NodeTaskJob(IServiceProvider serviceProvider,
        RED RED,
        string injectNodeId,
        ILogger<NodeTaskJob> logger)
    {
        _nodes = RED.Nodes;
        _RED = RED;
        _logger = logger;
        _serviceProvider = serviceProvider;

        InjectNodeId = injectNodeId;
        var injectNode = _nodes[injectNodeId].Node;
        FlowNodeId = _nodes[injectNode.Container].Id;
        NodesChainCount = NodeWireUtil.GetLinkedNodes(injectNode, _RED.BasicNodesDict).Count;
    }

    public async void Run(NodeMsg? msg = null)
    {
        var node = _nodes[InjectNodeId];
        node.RED = CreateContextForNode(InjectNodeId);
        _RED.OnNodeImplDone += _RED_OnNodeImplDone;

        _logger.LogInformation($"🔷 Run (TaskId={TaskId}) \n\tExecuteNode: {node.Node.DisplayName}({node.Node.Type}/{node.Id}");

        await ExecuteNode(msg ?? new(), node, new());
    }

    public void Terminate()
    {
        _logger.LogTrace($"Terminate (TaskId={TaskId})");
        _cancellationTokenSource.Cancel();

        IsTerminated = true;
        Finish();
    }

    void CallbackNext(string completedNodeId, NodeMsg result, int output)
    {
        var nextNodes = GetNextWires(completedNodeId, output);

        foreach (var _wire in nextNodes)
        {
            var node = _nodes[_wire.NodeId];
            var portIndex = _wire.PortIndex;
            if (node.Node.Disabled) continue;

            node.RED = CreateContextForNode(node.Id);
            _ = ExecuteNode(result, node, portIndex);
        }

    }

    private async Task ExecuteNode(NodeMsg input, INodeImplement node, int inputPortIndex)
    {
        if (_cancellationTokenSource.IsCancellationRequested) return;

        executedCount++;
        if (executedCount > maxExecuteCount)
        {
            _logger.LogError($"MAX_EXECUTE_COUNT={maxExecuteCount}");
            throw new Exception($"MAX_EXECUTE_COUNT={maxExecuteCount}");
        }

        var job = UpsertJob(node);
        var go = job.CreateExecutionStart();
        var isShort = executedCount <= 10;
        var left = isShort
                        ? new string('>', executedCount)
                        : (new string('>', 10) + $"({executedCount})");

        if (executedCount > 1)
            _logger.LogTrace($"🚃 {left} (TaskId={TaskId}) \n\tExecuteNode: {node.Node.DisplayName}({node.Node.Type}/{node.Id}");

        try
        {
            await node.Execute(input,
                (e, _output) =>
                {
                    //_logger.LogTrace($"call next wire = {node.Node.DisplayName}({node.Node.Type}/{node.Id})");
                    CallbackNext(node.Id, e, _output);
                },
                new ExecutionParameters(TaskId, go.JobGuid, InputPort: inputPortIndex, CancellationToken: _cancellationTokenSource.Token)
                );

            if (node is ISelfFinalizingNode) go.Pending();
            else go.Success();
        }
        catch (NodeExecuteException ex)
        {
            _logger.LogError(ex, "node execute exception");
            _RED.DebugMsg(node.Id, ex);
            go.Fail(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "node execute exception");
            _RED.DebugMsg(node.Id, ex);
            go.Fail(ex);
        }
        finally
        {
            Finalizer();
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

    NodeJob UpsertJob(INodeImplement node)
    {
        var job = _jobs.TryGetValue(node.Id, out var _job) ? _job : new(node);
        if (_job is null) _jobs.Add(node.Id, job);

        return job;
    }

    void Finalizer()
    {
        if (!isAllDone) return;

        Finish();
    }

    private void _RED_OnNodeImplDone(string nodeId, Guid jobGuid)
    {
        //TODO: тут есть проблема, event прячет exception,
        //  и если у NodeImpl будет Больше одного RED.Done() то должно возникать исключение, но этого не происходит

        if (_nodes[nodeId] is not ISelfFinalizingNode)
            throw new InvalidOperationException("RED.Done() - may use only :ISelfFinalizingNode ");

        var job = _jobs.GetValueOrDefault(nodeId)
                        ?? throw new InvalidOperationException("RED.Done() - job not found");

        var go = job.Executions.FirstOrDefault(s => s.JobGuid == jobGuid);
        if (go is null)
        {
            _logger.LogError("RED.Done() - job guid not found");
            throw new InvalidOperationException("RED.Done() - job guid not found");
        }

        go.Success();
        Finalizer();
    }

    void Finish()
    {
        if (IsTerminated)
            _logger.LogInformation($"🔴 Terminated! executedCount={executedCount}");
        else
            _logger.LogInformation($"✅ Finish! executedCount={executedCount}");

        OnComplete?.Invoke();
    }

    public ValueTask DisposeAsync()
    {
        _cancellationTokenSource.Dispose();

        _RED.OnNodeImplDone -= _RED_OnNodeImplDone;
        _logger.LogTrace($"Dispose");
        return ValueTask.CompletedTask;
    }

}
