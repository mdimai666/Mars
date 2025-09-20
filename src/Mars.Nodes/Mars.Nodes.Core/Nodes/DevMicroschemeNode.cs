using System.ComponentModel.DataAnnotations;

namespace Mars.Nodes.Core.Nodes;

#if DEBUG
[Display(GroupName = "dev")]
public class DevMicroschemeNode : Node
{
    public DevMicroschemeNode()
    {
        Color = "#1e1e1e";
        Outputs = [
            new() { Label = "out1" },
            new() { Label = "out2" },
            new() { Label = "out3" },
            new() { Label = "out4" },
            ];
        Inputs = [
            new() { Label = "in1" },
            new() { Label = "in2" },
            new() { Label = "in3" },
            new() { Label = "in4" },
        ];
        Icon = "_content/Mars.Nodes.Workspace/nodes/csproj-48.png";
    }
}

#endif
