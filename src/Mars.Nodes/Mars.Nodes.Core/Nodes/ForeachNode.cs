using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/ForeachNode/ForeachNode{.lang}.md")]
[Display(GroupName = "sequence")]
public class ForeachNode : Node
{
    public EForeachKind Kind { get; set; }

    [Display(Name = "Repeat count", Description = "leave empty for use payload:int")]
    public int? RepeatCount { get; set; }

    public ForeachNode()
    {
        Inputs = [new()];
        Color = "#cfcfcf";
        Outputs = new List<NodeOutput> {
            new NodeOutput(){ Label = "iterate" },
        };
        Icon = "_content/Mars.Nodes.Workspace/nodes/loop-start.svg";
    }

    public class ForeachCycle
    {
        public int index;
        public int count;
        public IEnumerable<object> arr = Array.Empty<object>();
    }
}

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/ForeachIterateNode/ForeachIterateNode{.lang}.md")]
[Display(GroupName = "sequence")]
public class ForeachIterateNode : Node
{
    public ForeachIterateNode()
    {
        Inputs = [new()];
        Color = "#cfcfcf";
        Outputs = new List<NodeOutput> {
            new NodeOutput() { Label = "finish" },
            new NodeOutput(){ Label = "iterate" },
        };
        Icon = "_content/Mars.Nodes.Workspace/nodes/loop.svg";
    }
}

public enum EForeachKind
{
    PayloadArray,
    Repeat
}
