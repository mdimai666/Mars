using Mars.PxBlocks.Runtime.Execution;
using Mars.PxBlocks.Runtime.Values;

namespace Mars.PxBlocks.Runtime.Standard;

internal sealed class StdMathNumber : PxExpressionImplement
{
    public override string TypeId => "core.math.number";

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
        => ValueTask.FromResult<PxValue>(new PxNumberValue(call.FieldNumber("NUM")));
}

internal sealed class StdText : PxExpressionImplement
{
    public override string TypeId => "core.text.text";

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
        => ValueTask.FromResult<PxValue>(new PxStringValue(call.FieldText("TEXT")));
}

internal sealed class StdLogicBoolean : PxExpressionImplement
{
    public override string TypeId => "core.logic.boolean";

    public override ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
        => ValueTask.FromResult<PxValue>(new PxBooleanValue(call.FieldText("BOOL") == "TRUE"));
}
