using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes.TaskNodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/TerminateAllJobsNode/TerminateAllJobsNode{.lang}.md")]
[Display(GroupName = "task")]
public class TerminateAllJobsNode : Node
{
    public override string TypeId => "core.TerminateAllJobsNode";

    public TerminateNodesScope Scope { get; set; }

    public TerminateAllJobsNode()
    {
        Inputs = [new()];
        Color = "#e16538";
        Icon = "_content/Mars.Nodes.Workspace/nodes/loop-start.svg";
    }
}

public enum TerminateNodesScope
{
    All = 0,
    Flow = 2
}
