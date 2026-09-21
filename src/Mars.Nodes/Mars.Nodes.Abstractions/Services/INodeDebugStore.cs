using Mars.Nodes.Core;

namespace Mars.Nodes.Abstractions.Services;

/// <summary>
/// Last messages nodes sent further. Snapshots are keyed by the node that emitted the message
/// and its output port — exactly the pair the suggestion provider walks by wire.
/// </summary>
public interface INodeDebugStore
{
    /// <summary>
    /// Builds and stores the snapshot synchronously (the message is not copied — a deferred build
    /// would race with downstream mutations of the payload). Throttled per node+port.
    /// Returns true when a snapshot was actually stored.
    /// </summary>
    bool Save(NodeMsg msg, string nodeId, int outputPort);

    IReadOnlyDictionary<string, NodeDebugSnapshot[]> Get(IReadOnlyCollection<string> nodeIds);

    /// <summary>
    /// Полный объект DebugNode (StoreFullObject): последний на ноду, жёсткий лимит 2 МБ,
    /// без TTL и независимо от глобального DebugMode. Throttled per node.
    /// </summary>
    bool SaveFull(string nodeId, object? value);

    NodeDebugFullSnapshot? GetFull(string nodeId);
}

/// <summary>Global debug mode; not persisted — after a restart it is off again.</summary>
public interface INodeDebugMode
{
    bool Enabled { get; set; }
}
