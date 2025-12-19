using System.ComponentModel.DataAnnotations;
using Mars.Nodes.Core;

namespace Mars.WebApp.Nodes.Nodes;

[Display(GroupName = "render")]
public class MarsHostRootLayoutRenderNode : Node
{
    public MarsHostRootLayoutRenderNode()
    {
        Inputs = [new()];
        Color = "#603dd7";
        Icon = "_content/Mars.Nodes.Workspace/nodes/razor-48.png";
        Outputs = [new() { Label = "html" }];
    }
}
