using System.Collections.Concurrent;
using System.Threading.Channels;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Utils;
using Mars.Nodes.Host.Services;
using Mars.Nodes.Host.Shared;
using Mars.Nodes.Host.Shared.Dto.NodeTasks;
using Mars.Nodes.Host.Shared.Services;
using Microsoft.Extensions.Logging;

namespace Mars.Nodes.Host.NodeTasks;

internal class NodeTaskJob : IAsyncDisposable
{
    public Guid TaskId { get; } = Guid.NewGuid();
    internal Node InjectNode => _nodes[InjectNodeId].Node;
    internal Node FlowNode => _nodes[FlowNodeId].Node;

    protected IServiceProvider _serviceProvider;
    protected readonly IReadOnlyDictionary<string, INodeImplement> _nodes;
    protected ConcurrentDictionary<string, NodeJob> _jobs = new();
    protected INodeRuntime _runtime;
    private readonly ILogger<NodeTaskJob>? _logger;

    private int executedCount;
    public int MaxExecuteCount { get; init; } = 2_000;

    private readonly Channel<NodeExecutionTask> _executionQueue;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly List<Task> _workerTasks = [];
    private readonly int _maxDegreeOfParallelism;
    private int _activeTasksCount = 0;
    private int _finishCalled = 0;

    public string InjectNodeId { get; }
    public int InjectPortIndex { get; }
    public string FlowNodeId { get; }

    public event Action? OnComplete;
    public event NodeExecutionHandler? OnNodeExecute;
    public event NodeExceptionHandler? OnNodeException;

    public int ExecuteCount => executedCount;
    public int NodesChainCount { get; }
    public IReadOnlyDictionary<string, NodeJob> Jobs => _jobs;
    public bool IsDone => _jobs.Values.All(s => s.IsDone) && _activeTasksCount == 0;
    public bool IsTerminated { get; private set; }
    public int ErrorCount => _jobs.Values.Sum(s => s.Executions.Count(x => x.Result == NodeJobExecutionResult.Fail));

    public DateTimeOffset StartDate { get; init; }
    public DateTimeOffset? EndDate { get; private set; }

    internal NodeTaskJob(IServiceProvider serviceProvider,
        INodeRuntime runtime,
        string injectNodeId,
        ILogger<NodeTaskJob>? logger,
        int injectPortIndex = 0,
        int maxDegreeOfParallelism = 10)
    {
        _nodes = runtime.Nodes;
        _runtime = runtime;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _maxDegreeOfParallelism = maxDegreeOfParallelism;

        StartDate = DateTimeOffset.Now;
        InjectNodeId = injectNodeId;
        InjectPortIndex = injectPortIndex;
        var injectNode = _nodes[injectNodeId].Node;
        FlowNodeId = _nodes[injectNode.Container].Id;
        NodesChainCount = NodeWireUtil.GetLinkedNodes(injectNode, _runtime.BasicNodesDict).Count;

        _executionQueue = Channel.CreateBounded<NodeExecutionTask>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        });
    }

    public async Task Run(NodeMsg msg, bool throwOnError = false)
    {
        var node = _nodes[InjectNodeId];
        node.RNS = CreateContextForNode(InjectNodeId);
        _runtime.OnNodeImplDone += _RNS_OnNodeImplDone;

        _logger?.LogInformation("🔷 Run (TaskId={TaskId}) ExecuteNode: {NodeName}({NodeType}/{NodeId}) [Parallel={Parallel}]",
            TaskId, node.Node.DisplayName, node.Node.Type, node.Id, _maxDegreeOfParallelism);

        // Запускаем воркеры
        for (int i = 0; i < _maxDegreeOfParallelism; i++)
        {
            _workerTasks.Add(WorkerAsync(i, throwOnError));
        }

        // Добавляем начальную задачу
        await EnqueueExecutionAsync(msg, node, InjectPortIndex, isInject: true, sourceOutputPortIndex: 0);

        // Ждем завершения всех воркеров
        await Task.WhenAll(_workerTasks);
    }

    private async Task WorkerAsync(int workerId, bool throwOnError)
    {
        try
        {
            await foreach (var task in _executionQueue.Reader.ReadAllAsync(_cancellationTokenSource.Token))
            {
                if (_cancellationTokenSource.IsCancellationRequested) break;

                _logger?.LogTrace("Worker {WorkerId} processing node {NodeId} (TaskId={TaskId})",
                    workerId, task.Node.Id, TaskId);

                await ExecuteNodeIterative(task, throwOnError);
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogTrace("Worker {WorkerId} cancelled (TaskId={TaskId})", workerId, TaskId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Worker {WorkerId} error (TaskId={TaskId})", workerId, TaskId);
            throw;
        }
    }

    private async Task EnqueueExecutionAsync(NodeMsg input, INodeImplement node, int inputPortIndex,
        bool isInject, int sourceOutputPortIndex)
    {
        var task = new NodeExecutionTask
        {
            Input = input,
            Node = node,
            InputPortIndex = inputPortIndex,
            IsInject = isInject,
            SourceOutputPortIndex = sourceOutputPortIndex
        };

        Interlocked.Increment(ref _activeTasksCount);
        await _executionQueue.Writer.WriteAsync(task, _cancellationTokenSource.Token);
    }

    private async Task ExecuteNodeIterative(NodeExecutionTask task, bool throwOnError)
    {
        if (_cancellationTokenSource.IsCancellationRequested) return;

        var node = task.Node;
        var job = _jobs.GetOrAdd(node.Id, id => new NodeJob(_nodes[id]));
        var go = job.CreateExecutionStart();
        var isSelfFinalizing = node is ISelfFinalizingNode;

        try
        {
            var count = Interlocked.Increment(ref executedCount);
            if (count >= MaxExecuteCount)
            {
                _logger?.LogError("MAX_EXECUTE_COUNT={MaxCount} (TaskId={TaskId})", MaxExecuteCount, TaskId);
                throw new Exception($"MAX_EXECUTE_COUNT={MaxExecuteCount}");
            }

            OnNodeExecute?.Invoke(node.Id, task.IsInject ? NodeExecutionTrigger.Inject : NodeExecutionTrigger.CallChain);

            async Task callbackNext(NodeMsg e, int output = 0)
            {
                if (_cancellationTokenSource.IsCancellationRequested) return;

                var nextNodes = GetNextWires(node.Id, output);
                foreach (var wire in nextNodes)
                {
                    var nextNode = _nodes[wire.NodeId];
                    if (nextNode.Node.Disabled) continue;

                    nextNode.RNS = CreateContextForNode(nextNode.Id);
                    var resultCopy = e.Copy();

                    await EnqueueExecutionAsync(resultCopy, nextNode, wire.PortIndex,
                        isInject: false, sourceOutputPortIndex: output);
                }
            }

            var executionParameters = new ExecutionParameters(
                TaskId: TaskId,
                JobGuid: go.JobGuid,
                InputPort: task.InputPortIndex,
                CancellationToken: _cancellationTokenSource.Token,
                SourceOutputPort: task.SourceOutputPortIndex);

            if (isSelfFinalizing)
                go.Pending();

            await node.Execute(task.Input,
                                async (e, output) => await callbackNext(e, output),
                                executionParameters);

            if (!isSelfFinalizing)
                go.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Node execute exception (TaskId={TaskId}, NodeId={NodeId})", TaskId, node.Id);
            _runtime?.DebugMsg(node.Id, ex);
            go.Fail(ex);
            OnNodeException?.Invoke(node.Id, FlowNodeId, ex);
            if (throwOnError) throw;
        }
        finally
        {
            var remaining = Interlocked.Decrement(ref _activeTasksCount);

            if (remaining == 0 && IsDone)
            {
                Finish();
                _executionQueue.Writer.Complete();
            }
        }
    }

    public void Terminate()
    {
        _logger?.LogTrace("Terminate (TaskId={TaskId})", TaskId);
        _cancellationTokenSource.Cancel();
        _executionQueue.Writer.Complete();

        IsTerminated = true;
        Finish();
    }

    public IRuntimeNodeScope CreateContextForNode(string nodeId)
    {
        var node = _nodes[nodeId];
        var flow = node is FlowNodeImpl ? node : _nodes[node.Node.Container];

        if (flow is not FlowNodeImpl flowImpl)
            throw new InvalidOperationException($"Node {node.Id} or its container {node.Node.Container} is not a FlowNodeImpl");

        return new RuntimeNodeScope(nodeId, flowImpl, _runtime, _serviceProvider);
    }

    IEnumerable<NodeWire> GetNextWires(string nodeId, int outputIndex)
    {
        var node = _nodes[nodeId];
        var outsWires = node.Node.Wires.ElementAtOrDefault(outputIndex);
        return outsWires ?? Enumerable.Empty<NodeWire>();
    }

    private async void _RNS_OnNodeImplDone(string nodeId, Guid jobGuid)
    {
        if (_nodes[nodeId] is not ISelfFinalizingNode)
            throw new InvalidOperationException("RNS.Done() - may use only :ISelfFinalizingNode");

        var job = _jobs.GetValueOrDefault(nodeId)
                        ?? throw new InvalidOperationException("RNS.Done() - job not found");

        var go = job.Executions.FirstOrDefault(s => s.JobGuid == jobGuid);
        if (go is null)
        {
            _logger?.LogError("RNS.Done() - job guid not found (TaskId={TaskId})", TaskId);
            throw new InvalidOperationException("RNS.Done() - job guid not found");
        }

        go.Success();

        if (IsDone)
        {
            Finish();
            _executionQueue.Writer.Complete();
        }
    }

    private void Finish()
    {
        if (Interlocked.Exchange(ref _finishCalled, 1) == 1) return;

        if (IsTerminated)
        {
            _logger?.LogInformation("🔴 Terminated! executedCount={Count} (TaskId={TaskId})", executedCount, TaskId);
            foreach (var job in _jobs.Values)
            {
                foreach (var execution in job.Executions)
                {
                    if (execution.End == null)
                    {
                        execution.Terminate();
                    }
                }
            }
        }
        else
        {
            _logger?.LogInformation("✅ Finish! executedCount={Count} (TaskId={TaskId})", executedCount, TaskId);
        }

        EndDate = DateTimeOffset.Now;
        OnComplete?.Invoke();
    }

    public ValueTask DisposeAsync()
    {
        _cancellationTokenSource.Dispose();
        _runtime.OnNodeImplDone -= _RNS_OnNodeImplDone;
        _logger?.LogTrace("Dispose (TaskId={TaskId})", TaskId);
        return ValueTask.CompletedTask;
    }
}

internal class NodeExecutionTask
{
    public required NodeMsg Input { get; init; }
    public required INodeImplement Node { get; init; }
    public required int InputPortIndex { get; init; }
    public required bool IsInject { get; init; }
    public required int SourceOutputPortIndex { get; init; }
}
