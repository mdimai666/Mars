using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes.Sequences;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/JoinNode/JoinNode{.lang}.md")]
[Display(GroupName = "sequence")]
public class JoinNode : Node
{
    public override string TypeId => "core.JoinNode";

    public JoinMode Mode { get; set; } = JoinMode.InputAggregation;
    public int InputAggregationTimeoutSeconds { get; set; } = 15;
    public int AggregationTimeSeconds { get; set; } = 5;
    public int MessageCount { get; set; } = 5;

    public JoinNode()
    {
        Color = "#E0D870";
        Icon = "_content/Mars.Nodes.Workspace/nodes/option.svg";
        Inputs = [new(), new()];
        Outputs = [new() { Label = "on join" }, new() { Label = "on InputAggregation timeout" }];
    }

    public enum JoinMode
    {
        /// <summary>
        /// Ждать сообщения со всех входных портов.
        /// </summary>
        InputAggregation = 0,

        /// <summary>
        /// Собрать N сообщений независимо от порта.
        /// </summary>
        CountAggregation = 1,

        /// <summary>
        /// Собирать сообщения до истечения таймаута.
        /// </summary>
        TimeAggregation = 2
    }
}
