using System.Collections.Concurrent;
using Mars.Nodes.Abstractions.Services;
using Mars.Nodes.Core;
using Microsoft.Extensions.Logging;

namespace Mars.Nodes.Host.Services;

/// <summary>
/// Global debug mode with auto-off: enabling starts a fixed window, after which the mode turns
/// itself off (lazily, on the next read). Not persisted — after a restart it is off again.
/// </summary>
internal class DebugModeState : INodeDebugMode
{
    public static readonly TimeSpan AutoOffAfter = TimeSpan.FromMinutes(30);

    DateTime _enabledUntil;

    internal Func<DateTime> Clock { get; set; } = () => DateTime.UtcNow;

    public bool Enabled
    {
        get => _enabledUntil != default && Clock() < _enabledUntil;
        set => _enabledUntil = value ? Clock() + AutoOffAfter : default;
    }
}

/// <summary>
/// In-memory snapshots with a TTL, written synchronously by the task executor for every message a node
/// sent further. The build is not deferred: a delayed snapshot would race with downstream payload
/// mutations. A per-key leading-edge throttle caps the build rate; messages inside the window are skipped.
/// Not <c>IMemoryCache</c>: it cannot enumerate keys, and the client asks for a set of node ids at once.
/// </summary>
internal class NodeDebugStore(INodeDebugMode debugMode, ILogger<NodeDebugStore>? logger = null) : INodeDebugStore
{
    public static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan ThrottleDelay = TimeSpan.FromMilliseconds(300);

    readonly ConcurrentDictionary<string, NodeDebugSnapshot> _snapshots = new();

    internal Func<DateTime> Clock { get; set; } = () => DateTime.UtcNow;

    public bool Save(NodeMsg msg, string nodeId, int outputPort)
    {
        if (!debugMode.Enabled || string.IsNullOrEmpty(nodeId)) return false;

        var key = NodeDebugSnapshot.Key(nodeId, outputPort);
        var now = Clock();

        if (_snapshots.TryGetValue(key, out var last) && now - last.CapturedAt < ThrottleDelay) return false;

        try
        {
            _snapshots[key] = NodeDebugSnapshotBuilder.Build(msg, nodeId, outputPort, now);
            return true;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "debug snapshot build failed (nodeId={NodeId}, port={Port})", nodeId, outputPort);
            return false;
        }
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
}
