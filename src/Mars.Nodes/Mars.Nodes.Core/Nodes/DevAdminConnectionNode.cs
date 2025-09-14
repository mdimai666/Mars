using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/DevAdminConnectionNode/DevAdminConnectionNode{.lang}.md")]
[Display(GroupName = "user")]
public class DevAdminConnectionNode : Node
{
    public string Action { get; set; } = ACTION_MESSAGE;

    public string Message { get; set; } = "";

    /// <summary>
    /// <see cref="Mars.Core.Models.MessageIntent"/>
    /// </summary>
    public string MessageIntent { get; set; } = "";

    public DevAdminConnectionNode()
    {
        Inputs = [new()];
        Color = "#3b9c9c";
        hasTailButton = false;
        Icon = "_content/Mars.Nodes.Workspace/nodes/info-circle.svg";
    }

    public static string[] Actions = [ACTION_MESSAGE];

    public const string ACTION_MESSAGE = "Message";
}
