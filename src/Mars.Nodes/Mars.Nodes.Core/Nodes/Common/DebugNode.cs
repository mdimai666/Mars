using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes.Common;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/DebugNode/DebugNode{.lang}.md")]
[Display(GroupName = "common")]
public class DebugNode : Node
{
    public override string TypeId => "core.DebugNode";

    public bool CompleteInputMessage { get; set; }
    public bool WriteToConsole { get; set; }
    public bool ShowPayloadTypeInStatus { get; set; }
    public override string Label => CompleteInputMessage ? "Msg" : base.Label;
    public Mars.Core.Models.MessageIntent? Level { get; set; }

    public DebugNode()
    {
        Inputs = [new()];
        Color = "#7AB073";
        hasTailButton = true;
        Icon = "_content/Mars.Nodes.Workspace/nodes/chat-left.svg";
    }

}
