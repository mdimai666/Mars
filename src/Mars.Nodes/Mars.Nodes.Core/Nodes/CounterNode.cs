using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Mars.Nodes.Core.Nodes;

[Display(GroupName = "diagnostic")]
public class CounterNode : Node
{
    [JsonConstructor]
    public CounterNode()
    {
        Color = "#A9BBCF";
        Outputs = [new() { Label = "output count" }];
        Inputs = [new() { Label = "substract" }, new() { Label = "increment" }];
        Icon = "_content/Mars.Nodes.Workspace/nodes/box-arrow-in-right.svg";
    }
}
