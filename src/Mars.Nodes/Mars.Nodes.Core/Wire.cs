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
    public string Node1 { get; set; } = default!;
    public string Node2 { get; set; } = default!;
    public int Node1Output { get; set; }

}

public class NewWire : Wire
{
    public bool IsOutput { get; set; }
}
