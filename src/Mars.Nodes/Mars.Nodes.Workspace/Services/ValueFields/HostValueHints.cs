using System.Collections.Concurrent;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Contracts.Nodes;
using Mars.Nodes.Front.Abstractions.Services;

namespace Mars.Nodes.Workspace.Services.ValueFields;

internal class HostValueHints : IHostValueHints
{
    static readonly OutputValueSpec[] Empty = [];

    IReadOnlyDictionary<string, OutputValueSpec[]> _outputSpecs = new Dictionary<string, OutputValueSpec[]>();
    readonly ConcurrentDictionary<string, NodeDebugSnapshot> _snapshots = new();

    DateTime _serverTimeUtc;
    DateTime _receivedAtUtc;

    public IReadOnlyCollection<string> GlobalVariableNames { get; private set; } = [];

    public int Version { get; private set; }

    public void SetOutputSpecs(IReadOnlyDictionary<string, OutputValueSpec[]> specs)
    {
        _outputSpecs = specs;
        Version++;
    }

    public void SetGlobalVariableNames(IReadOnlyCollection<string> names)
    {
        GlobalVariableNames = names;
        Version++;
    }

    public void SetDebugSnapshots(NodeDebugSnapshotsResponse response)
    {
        foreach (var (nodeId, nodeSnapshots) in response.Snapshots)
        {
            foreach (var snapshot in nodeSnapshots)
                _snapshots[NodeDebugSnapshot.Key(nodeId, snapshot.Port)] = snapshot;
        }

        _serverTimeUtc = response.ServerTimeUtc;
        _receivedAtUtc = DateTime.UtcNow;
        Version++;
    }

    public IReadOnlyCollection<OutputValueSpec> GetOutputSpecs(string nodeTypeId)
        => _outputSpecs.TryGetValue(nodeTypeId, out var specs) ? specs : Empty;

    public NodeDebugSnapshot? GetDebugSnapshot(string nodeId, int port)
        => _snapshots.TryGetValue(NodeDebugSnapshot.Key(nodeId, port), out var snapshot) ? snapshot : null;

    public TimeSpan GetSnapshotAge(DateTime capturedAtUtc)
        => _receivedAtUtc == default
            ? TimeSpan.MaxValue
            : (_serverTimeUtc - capturedAtUtc) + (DateTime.UtcNow - _receivedAtUtc);
}
