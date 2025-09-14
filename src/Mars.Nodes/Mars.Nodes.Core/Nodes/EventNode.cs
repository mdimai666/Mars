using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/EventNode/EventNode{.lang}.md")]
[Display(GroupName = "system")]
public class EventNode : Node
{
    public string Topics { get; set; } = "";

    public EventNode()
    {
        Color = "#e6e0f8";
        Outputs = new List<NodeOutput> { new NodeOutput() };
        Icon = "_content/Mars.Nodes.Workspace/nodes/activity.svg";
    }
}
