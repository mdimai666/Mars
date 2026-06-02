using System.Collections.Concurrent;
using Mars.Nodes.Core.Nodes;
using static Mars.Nodes.Core.Nodes.JoinNode;

namespace Mars.Nodes.Core.Implements.Nodes;

public class JoinNodeImpl : INodeImplement<JoinNode>, INodeImplement
{
    public JoinNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    private readonly ConcurrentDictionary<Guid, JoinState> _taskIdGroupedMessages = new();
    private readonly ConcurrentQueue<JoinInputMessage> _inputMessages = new();
    private readonly object _syncRoot = new();
    private CancellationTokenSource? _timeAggregationCts;

    public JoinNodeImpl(JoinNode node, IRED _RED)
    {
        Node = node;
        RED = _RED;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        return Node.Mode switch
        {
            JoinMode.InputAggregation => ExecuteForInputAggregation(input, callback, parameters),
            JoinMode.CountAggregation => ExecuteForCountAggregation(input, callback, parameters),
            JoinMode.TimeAggregation => ExecuteForTimeAggregation(input, callback, parameters),
            _ => throw new NotImplementedException()
        };
    }

    internal Task ExecuteForCountAggregation(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        var message = new JoinInputMessage(parameters.TaskId, input.Payload);
        _inputMessages.Enqueue(message);

        if (_inputMessages.Count >= Node.MessageCount)
        {
            var list = new List<object>();

            for (int i = 0; i < Node.MessageCount; i++)
            {
                if (_inputMessages.TryDequeue(out var item))
                    list.Add(item.Payload!);
            }

            callback(input.Copy(list.ToArray()));
        }

        return Task.CompletedTask;
    }

    internal Task ExecuteForTimeAggregation(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        if (input.Payload is JoinNodeTimeAggregationPackState packState)
        {
            callback(input.Copy(packState.Payload));
            return Task.CompletedTask;
        }

        if (Node.AggregationTimeSeconds < 1) throw new ArgumentException("AggregationTimeSeconds must be greater than 0");

        lock (_syncRoot)
        {
            var message = new JoinInputMessage(parameters.TaskId, input.Payload);
            _inputMessages.Enqueue(message);

            if (_timeAggregationCts == null)
            {
                _timeAggregationCts = new CancellationTokenSource();

                _ = RunTimeAggregation(
                    _timeAggregationCts);
            }
        }

        return Task.CompletedTask;
    }

    private async Task RunTimeAggregation(CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(
                TimeSpan.FromSeconds(Node.AggregationTimeSeconds),
                cts.Token);

            var list = new List<object>();

            while (_inputMessages.TryDequeue(out var item))
            {
                list.Add(item.Payload!);
            }

            if (list.Count > 0)
            {
                var payload = list.ToArray();
                KillTaskJobNodeImpl.CreateNewTask(this, Node.Id, new() { Payload = new JoinNodeTimeAggregationPackState { Payload = payload } });

            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            lock (_syncRoot)
            {
                _timeAggregationCts?.Dispose();
                _timeAggregationCts = null;
            }
        }
    }

    internal Task ExecuteForInputAggregation(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        var taskId = parameters.TaskId;
        var inputPort = parameters.InputPort;
        var inputCount = Node.Inputs.Count;

        if (input.Payload is JoinNodeTimeoutState timeoutState)
        {
            callback(input.Copy(timeoutState.Payload), output: 1);
            return Task.CompletedTask;
        }

        var state = _taskIdGroupedMessages.GetOrAdd(taskId, CreateState);

        state.Ports[inputPort] = input.Payload!;

        if (state.Ports.Count >= inputCount)
        {
            if (_taskIdGroupedMessages.TryRemove(taskId, out var completed))
            {
                completed.TimeoutCts?.Cancel();

                var payload = input.Copy(
                                    completed.Ports
                                        .OrderBy(x => x.Key)
                                        .Select(x => x.Value)
                                        .ToArray());

                completed.Dispose();

                callback(payload);
            }
        }

        return Task.CompletedTask;
    }

    private JoinState CreateState(Guid taskId)
    {
        var timeoutSeconds = Node.InputAggregationTimeoutSeconds;

        var state = new JoinState();

        if (timeoutSeconds > 0)
        {
            state = new JoinState
            {
                TimeoutCts = Node.InputAggregationTimeoutSeconds > 0
                                ? new CancellationTokenSource()
                                : null
            };

            _ = InputAggregationRemoveAfterTimeoutAsync(
                taskId,
                state,
                timeoutSeconds);
        }

        return state;
    }

    private async Task InputAggregationRemoveAfterTimeoutAsync(
        Guid taskId,
        JoinState state,
        int timeoutSeconds)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), state.TimeoutCts!.Token);

            if (_taskIdGroupedMessages.TryRemove(taskId, out var removed))
            {
                var payload = removed.Ports
                                .OrderBy(x => x.Key)
                                .Select(x => x.Value)
                                .ToArray();

                removed.Dispose();
                KillTaskJobNodeImpl.CreateNewTask(this, Node.Id, new() { Payload = new JoinNodeTimeoutState { TaskId = taskId, Payload = payload } });
                RED.DebugMsg(DebugMessage.NodeWarnMessage(Node.Id, $"timeout taskId:{taskId}"));
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private sealed class JoinState : IDisposable
    {
        public DateTime StartedUtc { get; } = DateTime.UtcNow;
        public CancellationTokenSource? TimeoutCts { get; init; }
        public ConcurrentDictionary<int, object> Ports { get; } = new();
        public void Dispose() => TimeoutCts?.Dispose();
    }

    private sealed class JoinNodeTimeoutState
    {
        public required Guid TaskId { get; init; }
        public required object Payload { get; init; }
    }

    private sealed class JoinNodeTimeAggregationPackState
    {
        public required object Payload { get; init; }
    }

    private sealed class JoinInputMessage
    {
        public DateTime StartedUtc { get; } = DateTime.UtcNow;
        public Guid TaskId { get; init; }
        public object? Payload { get; init; }

        public JoinInputMessage(Guid taskId, object? payload)
        {
            TaskId = taskId;
            Payload = payload;
        }
    }
}
