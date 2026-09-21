using Mars.Nodes.Core;

namespace Mars.Nodes.Abstractions.Services;

/// <summary>
/// Last messages nodes sent further. Snapshots are keyed by the node that emitted the message
/// and its output port — exactly the pair the suggestion provider walks by wire.
/// </summary>
public interface INodeDebugStore
{
    void Save(NodeMsg msg, string nodeId, int outputPort);

    IReadOnlyDictionary<string, NodeDebugSnapshot[]> Get(IReadOnlyCollection<string> nodeIds);
}

/// <summary>Global debug mode; not persisted — after a restart it is off again.</summary>
public interface INodeDebugMode
{
    bool Enabled { get; set; }
}
