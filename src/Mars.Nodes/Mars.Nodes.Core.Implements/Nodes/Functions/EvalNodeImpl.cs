using Mars.Nodes.Abstractions;
using Mars.Nodes.Expressions;

namespace Mars.Nodes.Core.Implements.Nodes.Functions;

public class EvalNodeImpl : INodeImplement<EvalNode>
{
    public EvalNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public EvalNodeImpl(EvalNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        using var expr = RNS.Expressions(Node);

        var result = expr.Resolve(Node.ValueKind, Node.Input, "", input, Node, "Input");

        input.Payload = result;

        callback(input);

        return Task.CompletedTask;
    }
}
