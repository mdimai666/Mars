namespace Mars.PxBlocks.Shared;

public class PxInput
{
    public string Name { get; set; } = "";
    public PxInputType Type { get; set; } = PxInputType.Value;
    public string? ConnectedBlockId { get; set; }
}

public enum PxInputType
{
    Value,
    Statement
}
