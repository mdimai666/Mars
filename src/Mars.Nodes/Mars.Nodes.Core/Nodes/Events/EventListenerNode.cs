using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes.Events;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/EventListenerNode/EventListenerNode{.lang}.md")]
[Display(GroupName = "events")]
public class EventListenerNode : Node
{
    public override string TypeId => "core.EventListenerNode";

    public string Topics { get; set; } = "";

    public EventListenerNode()
    {
        Color = "#e6e0f8";
        Outputs = new List<NodeOutput> { new NodeOutput() };
        Icon = "_content/Mars.Nodes.Workspace/nodes/activity.svg";
    }
}
