using Mars.Nodes.Abstractions;
using Mars.Nodes.Expressions;

namespace Mars.Nodes.Core.Implements.Nodes.Functions;

public class SwitchNodeImpl : INodeImplement<SwitchNode>
{
    public SwitchNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public SwitchNodeImpl(SwitchNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        using var expr = RNS.Expressions(Node);
        var someConditionIsTrue = false;

        for (var i = 0; i < Node.Conditions.Length; i++)
        {
            var condition = Node.Conditions[i];

            if (string.IsNullOrEmpty(condition.Value)) continue;
            if (condition.Value == SwitchNode.ElseConditionValue) continue;

            var result = (bool)expr.Resolve(condition.ValueKind, condition.Value, "bool", input, Node, $"Condition {i + 1}")!;

            if (!result) continue;

            someConditionIsTrue = true;
            callback(input, i);

            if (Node.BreakAfterFirst)
                break;
        }

        if (!someConditionIsTrue)
        {
            var @else = Node.Conditions.FirstOrDefault(s => s.Value == SwitchNode.ElseConditionValue);

            if (@else != null)
            {
                var elseIndex = Node.Conditions.IndexOf(@else);
                callback(input, elseIndex);
            }
        }

        return Task.CompletedTask;
    }
}
