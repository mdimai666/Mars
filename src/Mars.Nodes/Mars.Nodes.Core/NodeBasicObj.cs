namespace Mars.Nodes.Core;

public class NodeBasicObj : INodeBasic
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string TypeId { get; init; } = "";

    public float X { get; init; } = 0;
    public float Y { get; init; } = 0;
    public float Z { get; init; } = 0;

    public string Container { get; init; } = "";
    public bool Disabled { get; init; }

    public List<NodeInput> Inputs { get; set; } = [];
    public List<NodeOutput> Outputs { get; init; } = [];
    public List<string> OutputLabels { get; init; } = [];

    public List<List<NodeWire>> Wires { get; init; } = [];

    public bool IsConfigNode { get; init; }

}
