using Mars.Nodes.Core;

namespace Mars.Nodes.Workspace.Models;

public class PaletteNode
{
    public required Node Instance { get; set; }
    public required string GroupName { get; set; }
    public required string DisplayName { get; set; }
}
