using Mars.PxBlocks.Runtime.Execution;
using Mars.PxBlocks.Runtime.Values;

namespace Mars.PxBlocks.Runtime.Standard;

/// <summary>База стандартных блоков-выражений: TypeId обязателен к объявлению.</summary>
internal abstract class PxExpressionImplement : IPxExpressionImplement
{
    public abstract string TypeId { get; }

    public abstract ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call);
}

/// <summary>База стандартных блоков-операторов: TypeId обязателен к объявлению.</summary>
internal abstract class PxStatementImplement : IPxStatementImplement
{
    public abstract string TypeId { get; }

    public abstract Task ExecuteAsync(PxContext context, PxCall call);
}
