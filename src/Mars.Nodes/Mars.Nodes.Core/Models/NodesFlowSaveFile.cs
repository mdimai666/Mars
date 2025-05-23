namespace Mars.Nodes.Core.Models;

public class NodesFlowSaveFile
{
    public string Version { get; init; } = "1.0";
    public required Node[] Nodes { get; init; }
}
