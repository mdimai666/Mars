namespace Mars.Nodes.Core;

public class NodeBasicImplement : INodeBasic
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Type { get; init; } = "";

    public float X { get; init; } = 0;
    public float Y { get; init; } = 0;
    public float Z { get; init; } = 0;

    public string Container { get; init; } = "";
    public bool Disabled { get; init; }

    public List<NodeOutput> Outputs { get; init; } = new();
    public List<string> OutputLabels { get; init; } = new List<string>();

    public List<List<string>> Wires { get; init; } = new();

    public bool IsConfigNode { get; init; }

}
