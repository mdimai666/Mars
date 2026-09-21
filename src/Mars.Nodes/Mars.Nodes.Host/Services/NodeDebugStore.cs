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
    readonly ConcurrentDictionary<string, NodeDebugFullSnapshot> _fullSnapshots = new();

    internal Func<DateTime> Clock { get; set; } = () => DateTime.UtcNow;

    public bool Save(NodeMsg msg, string nodeId, int outputPort)
    {
        if (!debugMode.Enabled || string.IsNullOrEmpty(nodeId)) return false;

        var key = NodeDebugSnapshot.Key(nodeId, outputPort);

        return Throttled(key, _snapshots, () => NodeDebugSnapshotBuilder.Build(msg, nodeId, outputPort));
    }

    public bool SaveFull(string nodeId, object? value)
    {
        if (string.IsNullOrEmpty(nodeId)) return false;

        return Throttled(nodeId, _fullSnapshots, () => NodeDebugSnapshotBuilder.BuildFull(value, nodeId));
    }

    public NodeDebugFullSnapshot? GetFull(string nodeId)
        => _fullSnapshots.TryGetValue(nodeId, out var snapshot) ? snapshot : null;

    bool Throttled<TSnapshot>(string key, ConcurrentDictionary<string, TSnapshot> storage,
        Func<TSnapshot> build) where TSnapshot : class
    {
        var now = Clock();

        if (storage.TryGetValue(key, out var last) && now - CapturedAtOf(last) < ThrottleDelay) return false;

        try
        {
            storage[key] = build();
            return true;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "debug snapshot build failed (key={Key})", key);
            return false;
        }
    }

    static DateTime CapturedAtOf<TSnapshot>(TSnapshot snapshot) => snapshot switch
    {
        NodeDebugSnapshot s => s.CapturedAt,
        NodeDebugFullSnapshot s => s.CapturedAt,
        _ => throw new InvalidOperationException($"unexpected snapshot type {typeof(TSnapshot)}"),
    };

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
