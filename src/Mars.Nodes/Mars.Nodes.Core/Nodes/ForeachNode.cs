using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/ForeachNode/ForeachNode{.lang}.md")]
public class ForeachNode : Node
{
    public EForeachKind Kind { get; set; }

    [Display(Name = "Repeat count", Description = "leave empty for use payload:int")]
    public int? RepeatCount { get; set; }

    public ForeachNode()
    {
        haveInput = true;
        Color = "#cfcfcf";
        Outputs = new List<NodeOutput> {
            new NodeOutput(){ Label = "iterate" },
        };
        Icon = "_content/Mars.Nodes.Workspace/nodes/chunk-48.png";
    }

    public class ForeachCycle
    {
        public int index;
        public int count;
        public IEnumerable<object> arr = Array.Empty<object>();
    }
}

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/ForeachIterateNode/ForeachIterateNode{.lang}.md")]
public class ForeachIterateNode : Node
{
    public ForeachIterateNode()
    {
        haveInput = true;
        Color = "#cfcfcf";
        Outputs = new List<NodeOutput> {
            new NodeOutput(){ Label = "iterate" },
            new NodeOutput() { Label = "finish" },
        };
        Icon = "_content/Mars.Nodes.Workspace/nodes/chunk-48.png";
    }
}

public enum EForeachKind
{
    PayloadArray,
    Repeat
}
