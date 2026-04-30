using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;
using Mars.Core.Extensions;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/CatchErrorNode/CatchErrorNode{.lang}.md")]
[Display(GroupName = "task")]
public class CatchErrorNode : Node
{
    public override string DisplayName => Name.AsNullIfEmpty() ?? ("Catch " + Scope);

    public NodesErrorCatchScope Scope { get; set; } = NodesErrorCatchScope.Flow;

    public CatchErrorNode()
    {
        Outputs = [new() { Label = "On error" }];
        Color = "#e77c6d";
        Icon = "_content/Mars.Nodes.Workspace/nodes/loop-start.svg";
    }
}

public enum NodesErrorCatchScope
{
    All = 0,
    Flow = 1
}
