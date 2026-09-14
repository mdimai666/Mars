using System.Globalization;
using System.Text.Json;
using Mars.Nodes.Abstractions;
using Mars.Nodes.Core.Exceptions;

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
        foreach (var field in Node.Fields)
        {
            var value = ResolveValue(field);

            if (IsPayload(field))
                input.Payload = value;
            else
                input.Set(field.Key, value!);
        }

        callback(input);

        return Task.CompletedTask;
    }

    object? ResolveValue(InjectNodeField field)
    {
        if (field.VarType == VarNode.TimestampTypeName)
            return ResolveTimestamp(field);

        if (field.VarType == "string")
            return field.Value;

        try
        {
            return JsonSerializer.Deserialize(field.Value, VarNode.ResolveClrType(field.VarType));
        }
        catch (JsonException ex)
        {
            throw new NodeExecuteException(Node, $"Field '{field.Key}': value '{field.Value}' is not a valid {field.VarType}.", ex);
        }
    }

    long ResolveTimestamp(InjectNodeField field)
    {
        if (string.IsNullOrWhiteSpace(field.Value))
            return DateTimeOffset.Now.ToUnixTimeMilliseconds();

        if (long.TryParse(field.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var millis))
            return millis;

        throw new NodeExecuteException(Node, $"Field '{field.Key}': value '{field.Value}' is not a valid timestamp (empty or unix millis expected).");
    }

    static bool IsPayload(InjectNodeField field)
        => string.Equals(field.Key, InjectNode.PayloadKey, StringComparison.OrdinalIgnoreCase);
}
