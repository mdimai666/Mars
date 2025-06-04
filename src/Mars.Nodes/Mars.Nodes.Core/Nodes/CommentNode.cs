using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/CommentNode/CommentNode{.lang}.md")]
[Display(GroupName = "common")]
public class CommentNode : Node
{
    public string Text { get; set; } = "";

    public CommentNode()
    {
        Color = "#f5f4f4";
        Icon = "_content/Mars.Nodes.Workspace/nodes/chat.svg";
    }
}
