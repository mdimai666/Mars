using Mars.Nodes.Core;
using Mars.Nodes.Front.Abstractions.Services;

namespace Mars.Nodes.Workspace.Services.ValueFields;

internal class HostValueHints : IHostValueHints
{
    static readonly OutputValueSpec[] Empty = [];

    IReadOnlyDictionary<string, OutputValueSpec[]> _outputSpecs = new Dictionary<string, OutputValueSpec[]>();

    public IReadOnlyCollection<string> GlobalVariableNames { get; private set; } = [];

    public void SetOutputSpecs(IReadOnlyDictionary<string, OutputValueSpec[]> specs)
        => _outputSpecs = specs;

    public void SetGlobalVariableNames(IReadOnlyCollection<string> names)
        => GlobalVariableNames = names;

    public IReadOnlyCollection<OutputValueSpec> GetOutputSpecs(string nodeTypeId)
        => _outputSpecs.TryGetValue(nodeTypeId, out var specs) ? specs : Empty;
}
