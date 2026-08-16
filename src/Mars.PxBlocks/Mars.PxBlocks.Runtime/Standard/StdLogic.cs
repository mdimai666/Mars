using Mars.PxBlocks.Runtime.Execution;
using Mars.PxBlocks.Runtime.Values;

namespace Mars.PxBlocks.Runtime.Standard;

internal sealed class StdLogicNegate : PxExpressionImplement
{
    public StdLogicNegate() : base("core.logic.negate") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
        => ValueTask.FromResult<PxValue>(new PxBooleanValue(!call.Input("BOOL").IsTruthy()));
}

/// <summary>core.logic.compare: равенство структурное (по типу и значению), порядок — числа либо строки.</summary>
internal sealed class StdLogicCompare : PxExpressionImplement
{
    public StdLogicCompare() : base("core.logic.compare") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
    {
        var a = call.Input("A");
        var b = call.Input("B");
        var op = call.FieldText("OP");

        var result = op is "EQ" or "NEQ"
            ? op == "EQ" ? ValuesEqual(a, b) : !ValuesEqual(a, b)
            : Order(a, b, op);

        return ValueTask.FromResult<PxValue>(new PxBooleanValue(result));
    }

    /// <summary>Строки — лексикографически; остальное приводится к числу (с NaN все операции ложны).</summary>
    private static bool Order(PxValue a, PxValue b, string op)
    {
        if (a is PxStringValue leftText && b is PxStringValue rightText)
        {
            var order = string.CompareOrdinal(leftText.Value, rightText.Value);
            return op switch
            {
                "LT" => order < 0,
                "LTE" => order <= 0,
                "GT" => order > 0,
                "GTE" => order >= 0,
                _ => false
            };
        }

        var left = a.ToNumber();
        var right = b.ToNumber();
        return op switch
        {
            "LT" => left < right,
            "LTE" => left <= right,
            "GT" => left > right,
            "GTE" => left >= right,
            _ => false
        };
    }

    internal static bool ValuesEqual(PxValue a, PxValue b) => (a, b) switch
    {
        (PxNullValue, PxNullValue) => true,
        (PxNumberValue left, PxNumberValue right) => left.Number == right.Number,
        (PxBooleanValue left, PxBooleanValue right) => left.Value == right.Value,
        (PxStringValue left, PxStringValue right) => left.Value == right.Value,
        (PxListValue left, PxListValue right) => left.Items.Count == right.Items.Count
            && left.Items.Zip(right.Items).All(pair => ValuesEqual(pair.First, pair.Second)),
        (PxObjectValue left, PxObjectValue right) => ReferenceEquals(left, right),
        _ => false
    };
}
