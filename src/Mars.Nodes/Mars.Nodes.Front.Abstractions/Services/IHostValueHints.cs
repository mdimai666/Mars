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

    IReadOnlyCollection<OutputValueSpec> GetOutputSpecs(string nodeTypeId);
    IReadOnlyCollection<string> GlobalVariableNames { get; }
}
