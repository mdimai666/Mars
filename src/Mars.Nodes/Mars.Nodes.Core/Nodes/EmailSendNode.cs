using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/NodeFormEditor/Docs/EmailSendNode/EmailSendNode{.lang}.md")]
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
        haveInput = true;
        Color = "#cce8c0";
        Outputs = new List<NodeOutput> { new NodeOutput() };
        Icon = "_content/NodeWorkspace/nodes/envelope-48.png";
    }

}
