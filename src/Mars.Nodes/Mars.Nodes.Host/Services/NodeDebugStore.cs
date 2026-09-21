using System.Collections.Concurrent;
using Mars.Nodes.Abstractions.Services;
using Mars.Nodes.Core;
using Mars.Nodes.Host.Helpers;

namespace Mars.Nodes.Host.Services;

internal class DebugModeState : INodeDebugMode
{
    public bool Enabled { get; set; }
}

/// <summary>
/// In-memory snapshots with a TTL, written by the task executor for every message a node sent further.
/// Not <c>IMemoryCache</c>: it cannot enumerate keys, and the client asks for a set of node ids at once.
/// </summary>
internal class NodeDebugStore(INodeDebugMode debugMode) : INodeDebugStore
{
    public static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);
    static readonly TimeSpan ThrottleDelay = TimeSpan.FromMilliseconds(300);

    readonly ConcurrentDictionary<string, NodeDebugSnapshot> _snapshots = new();
    readonly SmartThrottleByKey _throttle = new(ThrottleDelay);

    internal Func<DateTime> Clock { get; set; } = () => DateTime.Now;

    public void Save(NodeMsg msg, string nodeId, int outputPort)
    {
        if (!debugMode.Enabled || string.IsNullOrEmpty(nodeId)) return;

        var key = Key(nodeId, outputPort);

        _throttle.TryExecute(key, () => _snapshots[key] = NodeDebugSnapshotBuilder.Build(msg, nodeId, outputPort));
    }

    public IReadOnlyDictionary<string, NodeDebugSnapshot[]> Get(IReadOnlyCollection<string> nodeIds)
    {
        var cutoff = Clock() - Ttl;
        var wanted = nodeIds.ToHashSet();
        var result = new Dictionary<string, NodeDebugSnapshot[]>();

        foreach (var (key, snapshot) in _snapshots)
        {
            if (snapshot.CapturedAt < cutoff)
            {
                _snapshots.TryRemove(key, out _);
                continue;
            }

            if (!wanted.Contains(snapshot.NodeId)) continue;

            result[snapshot.NodeId] = result.TryGetValue(snapshot.NodeId, out var list)
                ? [.. list, snapshot]
                : [snapshot];
        }

        return result;
    }

    static string Key(string nodeId, int port) => $"{nodeId}|{port}";
}
