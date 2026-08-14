using Mars.PxBlocks.Runtime.Execution;
using Mars.PxBlocks.Runtime.Values;

namespace Mars.PxBlocks.Runtime.Standard;

/// <summary>База стандартных блоков-выражений.</summary>
internal abstract class PxExpressionImplement(string typeId) : IPxExpressionImplement
{
    public string TypeId { get; } = typeId;

    public abstract ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call);
}

/// <summary>База стандартных блоков-операторов.</summary>
internal abstract class PxStatementImplement(string typeId) : IPxStatementImplement
{
    public string TypeId { get; } = typeId;

    public abstract Task ExecuteAsync(PxContext context, PxCall call);
}
