using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes.Sequences;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/docs/ForeachNode/ForeachNode{.lang}.md")]
[Display(GroupName = "sequence")]
public class ForeachNode : Node, INodeOutputValueSpec
{
    public override string TypeId => "core.ForeachNode";

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
        Outputs = [
            new NodeOutput(){ Label = "Finish" },
            new NodeOutput(){ Label = "Iterate" },
        ];
        Icon = "_content/Mars.Nodes.Workspace/nodes/foreach.svg";
    }

    /// <summary>Пути ForeachCycle объявлены вручную: у цикла public поля, а экспандер ходит только свойства.</summary>
    public IEnumerable<OutputValueSpec> GetOutputValueSpec()
    {
        yield return new OutputValueSpec(nameof(NodeMsg.Payload), "int", 0, "items count");
        yield return new OutputValueSpec(nameof(NodeMsg.Payload), VarNode.ObjectTypeName, 1, "current item");
        yield return new OutputValueSpec(nameof(ForeachCycle), VarNode.ObjectTypeName, OutputValueSpec.AllOutputPorts);
        yield return new OutputValueSpec($"{nameof(ForeachCycle)}.index", "int", OutputValueSpec.AllOutputPorts);
        yield return new OutputValueSpec($"{nameof(ForeachCycle)}.count", "int", OutputValueSpec.AllOutputPorts);
        yield return new OutputValueSpec($"{nameof(ForeachCycle)}.arr", "object[]", OutputValueSpec.AllOutputPorts);
    }

    public class ForeachCycle
    {
        public int index;
        public int count;
        public object[] arr = Array.Empty<object>();
    }
}

public enum EForeachKind
{
    PayloadArray,
    Repeat
}
