using Mars.PxBlocks.Shared.Toolbox;

namespace Mars.PxBlocks.Shared;

public class PxWorkspaceState
{
    public string BlocksJson { get; set; } = "";
    public PxToolbox Toolbox { get; set; } = new();
    public float Zoom { get; set; } = 1f;
    public float ScrollX { get; set; }
    public float ScrollY { get; set; }
}
