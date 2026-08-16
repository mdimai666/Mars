using Mars.PxBlocks.Runtime.Execution;
using Mars.PxBlocks.Runtime.Values;

namespace Mars.PxBlocks.Runtime.Standard;

internal sealed class StdMathArithmetic : PxExpressionImplement
{
    public StdMathArithmetic() : base("core.math.arithmetic") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
    {
        var a = call.Input("A").ToNumber();
        var b = call.Input("B").ToNumber();

        var result = call.FieldText("OP") switch
        {
            "ADD" => a + b,
            "MINUS" => a - b,
            "MULTIPLY" => a * b,
            "DIVIDE" => a / b,
            "POWER" => Math.Pow(a, b),
            _ => double.NaN
        };

        return ValueTask.FromResult<PxValue>(new PxNumberValue(result));
    }
}

internal sealed class StdMathSingle : PxExpressionImplement
{
    public StdMathSingle() : base("core.math.single") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
    {
        var value = call.Input("NUM").ToNumber();

        var result = call.FieldText("OP") switch
        {
            "ROOT" => Math.Sqrt(value),
            "ABS" => Math.Abs(value),
            "-" => -value,
            "LN" => Math.Log(value),
            "LOG10" => Math.Log10(value),
            "EXP" => Math.Exp(value),
            "POW10" => Math.Pow(10, value),
            _ => double.NaN
        };

        return ValueTask.FromResult<PxValue>(new PxNumberValue(result));
    }
}

/// <summary>core.math.trig: тригонометрия Blockly работает в градусах.</summary>
internal sealed class StdMathTrig : PxExpressionImplement
{
    public StdMathTrig() : base("core.math.trig") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
    {
        var degrees = call.Input("NUM").ToNumber();
        var radians = degrees * Math.PI / 180;

        var result = call.FieldText("OP") switch
        {
            "SIN" => Math.Sin(radians),
            "COS" => Math.Cos(radians),
            "TAN" => Math.Tan(radians),
            "ASIN" => Math.Asin(degrees) * 180 / Math.PI,
            "ACOS" => Math.Acos(degrees) * 180 / Math.PI,
            "ATAN" => Math.Atan(degrees) * 180 / Math.PI,
            _ => double.NaN
        };

        return ValueTask.FromResult<PxValue>(new PxNumberValue(result));
    }
}

internal sealed class StdMathConstant : PxExpressionImplement
{
    public StdMathConstant() : base("core.math.constant") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
    {
        var result = call.FieldText("CONSTANT") switch
        {
            "PI" => Math.PI,
            "E" => Math.E,
            "GOLDEN_RATIO" => (1 + Math.Sqrt(5)) / 2,
            "SQRT2" => Math.Sqrt(2),
            "SQRT1_2" => Math.Sqrt(0.5),
            "INFINITY" => double.PositiveInfinity,
            _ => double.NaN
        };

        return ValueTask.FromResult<PxValue>(new PxNumberValue(result));
    }
}

internal sealed class StdMathNumberProperty : PxExpressionImplement
{
    public StdMathNumberProperty() : base("core.math.number_property") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
    {
        var value = call.Input("NUMBER_TO_CHECK").ToNumber();

        var result = call.FieldText("PROPERTY") switch
        {
            "EVEN" => value % 2 == 0,
            "ODD" => value % 2 != 0,
            "PRIME" => IsPrime(value),
            "WHOLE" => Math.Floor(value) == value && !double.IsInfinity(value),
            "POSITIVE" => value > 0,
            "NEGATIVE" => value < 0,
            "DIVISIBLE_BY" => value % call.Input("DIVISOR").ToNumber() == 0,
            _ => false
        };

        return ValueTask.FromResult<PxValue>(new PxBooleanValue(result));
    }

    private static bool IsPrime(double value)
    {
        if (value < 2 || Math.Floor(value) != value)
            return false;

        var n = (long)value;
        if (n % 2 == 0)
            return n == 2;

        for (long divisor = 3; divisor * divisor <= n; divisor += 2)
        {
            if (n % divisor == 0)
                return false;
        }

        return true;
    }
}

internal sealed class StdMathRound : PxExpressionImplement
{
    public StdMathRound() : base("core.math.round") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
    {
        var value = call.Input("NUM").ToNumber();

        var result = call.FieldText("OP") switch
        {
            "ROUND" => Math.Round(value, MidpointRounding.AwayFromZero),
            "ROUNDUP" => Math.Ceiling(value),
            "ROUNDDOWN" => Math.Floor(value),
            _ => double.NaN
        };

        return ValueTask.FromResult<PxValue>(new PxNumberValue(result));
    }
}

internal sealed class StdMathModulo : PxExpressionImplement
{
    public StdMathModulo() : base("core.math.modulo") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
    {
        var dividend = call.Input("DIVIDEND").ToNumber();
        var divisor = call.Input("DIVISOR").ToNumber();
        return ValueTask.FromResult<PxValue>(new PxNumberValue(dividend % divisor));
    }
}

internal sealed class StdMathRandomInt : PxExpressionImplement
{
    public StdMathRandomInt() : base("core.math.random_int") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
    {
        // Алгоритм Blockly: floor(random * (to - from + 1) + from), границы включительно.
        var from = Math.Floor(call.Input("FROM").ToNumber());
        var to = Math.Floor(call.Input("TO").ToNumber());
        var value = Math.Floor(context.Random.NextDouble() * (to - from + 1) + from);
        return ValueTask.FromResult<PxValue>(new PxNumberValue(value));
    }
}

internal sealed class StdMathRandomFloat : PxExpressionImplement
{
    public StdMathRandomFloat() : base("core.math.random_float") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
        => ValueTask.FromResult<PxValue>(new PxNumberValue(context.Random.NextDouble()));
}
