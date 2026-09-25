using Mars.Nodes.Contracts.Nodes;
using Mars.Nodes.Core;

namespace Mars.Nodes.Front.Abstractions.Services;

/// <summary>
/// What only the host knows: output specs of nodes whose assemblies are not loaded into the front,
/// and names of live global variables. Filled once from Load().
/// </summary>
public interface IHostValueHints
{
    void SetOutputSpecs(IReadOnlyDictionary<string, OutputValueSpec[]> specs);
    void SetGlobalVariableNames(IReadOnlyCollection<string> names);

    /// <summary>Дополняет кэш снимков (pull может быть частичным — только нужные ноды).</summary>
    void SetDebugSnapshots(NodeDebugSnapshotsResponse response);

    IReadOnlyCollection<OutputValueSpec> GetOutputSpecs(string nodeTypeId);
    IReadOnlyCollection<string> GlobalVariableNames { get; }
    NodeDebugSnapshot? GetDebugSnapshot(string nodeId, int port);

    /// <summary>Возраст снимка по серверным часам (часы клиента могут отличаться).</summary>
    TimeSpan GetSnapshotAge(DateTime capturedAtUtc);

    /// <summary>Растёт при каждом Set — по нему потребитель видит, что кэш подсказок устарел.</summary>
    int Version { get; }
}
