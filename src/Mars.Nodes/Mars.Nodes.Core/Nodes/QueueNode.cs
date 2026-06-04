using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/QueueNode/QueueNode{.lang}.md")]
[Display(GroupName = "sequence")]
public class QueueNode : Node
{
    public EQueueMode Mode { get; set; } = EQueueMode.FIFO;

    [Display(Description = "Количество одновременных задач")]
    public int MaxTask { get; set; } = 1;

    public QueueNode()
    {
        Inputs = [
            new() {  Label = "Start"},
            new() {  Label = "NextStep"},
            ];
        Color = "#cfcfcf";
        Outputs = [
            new (){ Label = "Finish" },
            new (){ Label = "Iterate" },
        ];
        Icon = "_content/Mars.Nodes.Workspace/nodes/loop.svg";
    }

    public enum EQueueMode
    {
        FIFO,
        LIFO
    }
}
