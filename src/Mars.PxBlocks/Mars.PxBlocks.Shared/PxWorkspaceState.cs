namespace Mars.PxBlocks.Shared;

public class PxWorkspaceState
{
    public List<PxBlock> Blocks { get; set; } = [];
    public List<PxToolboxCategory> Toolbox { get; set; } = [];
    public float Zoom { get; set; } = 1f;
    public float ScrollX { get; set; }
    public float ScrollY { get; set; }
}
