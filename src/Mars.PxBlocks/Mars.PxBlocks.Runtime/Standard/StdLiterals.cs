using Mars.PxBlocks.Runtime.Execution;
using Mars.PxBlocks.Runtime.Values;

namespace Mars.PxBlocks.Runtime.Standard;

internal sealed class StdMathNumber : PxExpressionImplement
{
    public StdMathNumber() : base("math_number") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
        => ValueTask.FromResult<PxValue>(new PxNumberValue(call.FieldNumber("NUM")));
}

internal sealed class StdText : PxExpressionImplement
{
    public StdText() : base("text") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
        => ValueTask.FromResult<PxValue>(new PxStringValue(call.FieldText("TEXT")));
}

internal sealed class StdLogicBoolean : PxExpressionImplement
{
    public StdLogicBoolean() : base("logic_boolean") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
        => ValueTask.FromResult<PxValue>(new PxBooleanValue(call.FieldText("BOOL") == "TRUE"));
}
