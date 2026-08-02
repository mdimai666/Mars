namespace Mars.PxBlocks.Shared;

public class PxBlockDefinition
{
    public string TypeId { get; set; } = "";
    public string Message { get; set; } = "";
    public string Colour { get; set; } = "#A8A8A8";
    public string Tooltip { get; set; } = "";

    public bool PreviousStatement { get; set; }
    public bool NextStatement { get; set; }
    public bool Output { get; set; }
    public string? OutputType { get; set; }

    public Func<PxBlock>? Factory { get; set; }

    public PxBlock CreateBlock()
    {
        if (Factory != null)
            return Factory();

        PxBlock block = PreviousStatement || NextStatement
            ? Output
                ? throw new InvalidOperationException("Block cannot be both statement and output")
                : new PxBlockCommand()
            : Output
                ? new PxBlockValue { OutputType = OutputType ?? "string" }
                : new PxBlockHat();

        block.Color = Colour;
        return block;
    }
}
