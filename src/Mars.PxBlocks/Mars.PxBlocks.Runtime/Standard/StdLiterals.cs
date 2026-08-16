using Mars.PxBlocks.Runtime.Execution;
using Mars.PxBlocks.Runtime.Values;

namespace Mars.PxBlocks.Runtime.Standard;

internal sealed class StdMathNumber : PxExpressionImplement
{
    public StdMathNumber() : base("core.math.number") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
        => ValueTask.FromResult<PxValue>(new PxNumberValue(call.FieldNumber("NUM")));
}

internal sealed class StdText : PxExpressionImplement
{
    public StdText() : base("core.text.text") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
        => ValueTask.FromResult<PxValue>(new PxStringValue(call.FieldText("TEXT")));
}

internal sealed class StdLogicBoolean : PxExpressionImplement
{
    public StdLogicBoolean() : base("core.logic.boolean") { }

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
        => ValueTask.FromResult<PxValue>(new PxBooleanValue(call.FieldText("BOOL") == "TRUE"));
}
