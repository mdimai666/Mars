namespace Mars.PxBlocks.Shared;

public class PxToolboxCategory
{
    public string Name { get; set; } = "";
    public string Colour { get; set; } = "#A8A8A8";
    public string Icon { get; set; } = "";
    public bool Expanded { get; set; } = true;
    public List<PxBlockDefinition> Blocks { get; set; } = [];
}
