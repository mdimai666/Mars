using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/DevAdminConnectionNode/DevAdminConnectionNode{.lang}.md")]
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
        HaveInput = true;
        Color = "#3b9c9c";
        hasTailButton = false;
    }

    public static string[] Actions = [ACTION_MESSAGE];

    public const string ACTION_MESSAGE = "Message";
}
