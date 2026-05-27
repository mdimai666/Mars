using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Models;

public class NodesFlowSaveFile
{
    public string Version { get; init; } = "1.0";
    public required Node[] Nodes { get; init; }
}

public class NodesData
{
    public required Node[] Nodes { get; init; }
    public required InlineFunctionNodeSchema[] InlineFunctionNodeSchemas { get; init; }
}
