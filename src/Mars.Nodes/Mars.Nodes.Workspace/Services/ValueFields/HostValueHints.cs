using System.Collections.Concurrent;
using Mars.Nodes.Core;
using Mars.Nodes.Front.Abstractions.Services;

namespace Mars.Nodes.Workspace.Services.ValueFields;

internal class HostValueHints : IHostValueHints
{
    static readonly OutputValueSpec[] Empty = [];

    IReadOnlyDictionary<string, OutputValueSpec[]> _outputSpecs = new Dictionary<string, OutputValueSpec[]>();
    readonly ConcurrentDictionary<string, NodeDebugSnapshot> _snapshots = new();

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

    public void SetDebugSnapshots(IReadOnlyDictionary<string, NodeDebugSnapshot[]> snapshots)
    {
        _snapshots.Clear();

        foreach (var (nodeId, nodeSnapshots) in snapshots)
        {
            foreach (var snapshot in nodeSnapshots)
                _snapshots[Key(nodeId, snapshot.Port)] = snapshot;
        }

        Version++;
    }

    public IReadOnlyCollection<OutputValueSpec> GetOutputSpecs(string nodeTypeId)
        => _outputSpecs.TryGetValue(nodeTypeId, out var specs) ? specs : Empty;

    public NodeDebugSnapshot? GetDebugSnapshot(string nodeId, int port)
        => _snapshots.TryGetValue(Key(nodeId, port), out var snapshot) ? snapshot : null;

    static string Key(string nodeId, int port) => $"{nodeId}|{port}";
}
