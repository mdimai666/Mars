using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes.Common;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/LinkInNode/LinkInNode{.lang}.md")]
[Display(GroupName = "common")]
public class LinkInNode : Node
{
    public override string TypeId => "core.LinkInNode";

    public string[] OutLinksIds { get; set; } = [];

    public LinkInNode()
    {
        Color = "#dddddd";
        Inputs = [new()];
        Icon = "_content/Mars.Nodes.Workspace/nodes/box-arrow-in-right.svg";
    }
}

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/LinkOutNode/LinkOutNode{.lang}.md")]
[Display(GroupName = "common")]
public class LinkOutNode : Node
{
    public override string TypeId => "core.LinkOutNode";

    public LinkOutNode()
    {
        Color = "#dddddd";
        Outputs = [new()];
        Icon = "_content/Mars.Nodes.Workspace/nodes/box-arrow-in-right.svg";
    }
}
