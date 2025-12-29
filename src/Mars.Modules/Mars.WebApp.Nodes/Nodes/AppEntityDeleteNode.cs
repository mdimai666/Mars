using System.ComponentModel.DataAnnotations;
using Mars.Nodes.Core;

namespace Mars.WebApp.Nodes.Nodes;

[Display(GroupName = "entity")]
public class AppEntityDeleteNode : Node
{

    public AppEntityDeleteNode()
    {
        Color = "#21a366";
        Icon = "_content/Mars.Nodes.Workspace/nodes/db-48.png";
        Inputs = [new()];
        Outputs = [
            new (){ Label = "Deleted count" },
            new (){ Label = "Deleted ids" },
        ];
    }
}
