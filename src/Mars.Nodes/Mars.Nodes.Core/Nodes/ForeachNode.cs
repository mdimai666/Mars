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
        Inputs = [
            new() {  Label = "Start"},
            new() {  Label = "NextStep"},
            ];
        Color = "#cfcfcf";
        Outputs = new List<NodeOutput> {
            new NodeOutput(){ Label = "Finish" },
            new NodeOutput(){ Label = "Iterate" },
        };
        Icon = "_content/Mars.Nodes.Workspace/nodes/loop.svg";
    }

    public class ForeachCycle
    {
        public int index;
        public int count;
        public IEnumerable<object> arr = Array.Empty<object>();
    }
}

public enum EForeachKind
{
    PayloadArray,
    Repeat
}
