using DynamicExpresso;
using Mars.Nodes.Abstractions;
using Mars.Nodes.Core.Exceptions;
using Mars.Nodes.Core.Implements.Utils;

namespace Mars.Nodes.Core.Implements.Nodes.Common;

public class InjectNodeImpl : INodeImplement<InjectNode>
{
    public InjectNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public InjectNodeImpl(InjectNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        Interpreter? interpreter = null;

        foreach (var field in Node.Fields)
        {
            if (field.ValueKind == InputValueKind.Expression)
                interpreter ??= InputValueResolver.CreateInterpreter(RNS, input);

            var value = InputValueResolver.Resolve(field.ValueKind, field.Value, field.VarType, interpreter, new ExpressionScope(RNS, input), Node, $"Field '{field.Key}'");

            if (IsPayload(field))
                input.Payload = value;
            else
                input.Set(field.Key, value!);
        }

        callback(input);

        return Task.CompletedTask;
    }

    static bool IsPayload(InjectNodeField field)
        => string.Equals(field.Key, InjectNode.PayloadKey, StringComparison.OrdinalIgnoreCase);
}
