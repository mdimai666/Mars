using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/DebugNode/DebugNode{.lang}.md")]
[Display(GroupName = "common")]
public class DebugNode : Node
{
    public bool CompleteInputMessage { get; set; } = false;
    public bool ShowPayloadTypeInStatus { get; set; } = false;
    public override string Label => CompleteInputMessage ? "Msg" : base.Label;
    public Mars.Core.Models.MessageIntent? Level { get; set; }

    public DebugNode()
    {
        Inputs = [new()];
        Color = "#7AB073";
        hasTailButton = true;
        Icon = "_content/Mars.Nodes.Workspace/nodes/chat-left.svg";
    }

    //public async override Task Execute(object input, Action<object> callback, Action<object> Error)

}
