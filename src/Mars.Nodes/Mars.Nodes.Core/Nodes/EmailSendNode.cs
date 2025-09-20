using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/EmailSendNode/EmailSendNode{.lang}.md")]
[Display(GroupName = "network")]
public class EmailSendNode : Node
{
    public InputConfig<SmtpConfigNode> Config { get; set; }

    [Required]
    public string ToEmail { get; set; } = "";

    [Required]
    public string Subject { get; set; } = "";
    public string Message { get; set; } = "";

    public EmailSendNode()
    {
        Inputs = [new()];
        Color = "#cce8c0";
        Outputs = new List<NodeOutput> { new NodeOutput() };
        Icon = "_content/Mars.Nodes.Workspace/nodes/envelope-48.png";
    }

}
