using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes.Common;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/CommentNode/CommentNode{.lang}.md")]
[Display(GroupName = "common")]
public class CommentNode : Node
{
    public override string TypeId => "core.CommentNode";

    public string Text { get; set; } = "";

    public override float BodyRectWidth => Math.Min(360, Math.Max(120, Text.Length * 9 + 40));
    public override float BodyRectHeight => Math.Max(30, Math.Min(200, (Text.Length * 120) / 190));

    public CommentNode()
    {
        Color = "#f5f4f4";
        Icon = "_content/Mars.Nodes.Workspace/nodes/chat.svg";
    }
}
