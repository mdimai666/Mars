namespace Mars.Nodes.Core;

public class Wire
{
    public string Id { get; set; } = default!;
    public float X1 { get; set; }
    public float Y1 { get; set; }
    public float X2 { get; set; }
    public float Y2 { get; set; }
    public bool Selected { get; set; }
    public bool Disable { get; set; }
    public NodeWire Node1 { get; set; } = default!;
    public NodeWire Node2 { get; set; } = default!;

}

public class NewWire : Wire
{
    public bool IsOutput { get; set; }
}
