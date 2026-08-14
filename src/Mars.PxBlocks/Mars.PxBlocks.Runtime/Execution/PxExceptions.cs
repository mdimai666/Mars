using Mars.PxBlocks.Runtime.Ast;

namespace Mars.PxBlocks.Runtime.Execution;

/// <summary>Ошибка разбора workspace JSON. BlockId — блок, в котором найдена проблема.</summary>
public class PxParseException : Exception
{
    public string? BlockId { get; }

    public PxParseException(string message, string? blockId = null) : base(message)
        => BlockId = blockId;
}

/// <summary>Ошибка исполнения; несёт id блока для маппинга «ошибка → блок» в редакторе.</summary>
public class PxRuntimeException : Exception
{
    public string? BlockId { get; }

    public PxRuntimeException(string message, string? blockId = null) : base(message)
        => BlockId = blockId;
}

/// <summary>Превышен лимит шагов — защита от бесконечных циклов.</summary>
public sealed class PxStepLimitExceededException : PxRuntimeException
{
    public long Steps { get; }

    public PxStepLimitExceededException(string? blockId, long steps)
        : base($"Превышен лимит шагов исполнения ({steps})", blockId)
        => Steps = steps;
}

/// <summary>Служебный сигнал break/continue — разворачивает стек до ближайшего цикла.</summary>
internal sealed class PxFlowSignal : Exception
{
    public PxFlowKind Kind { get; }

    public PxFlowSignal(PxFlowKind kind) => Kind = kind;
}

/// <summary>Служебный сигнал return — разворачивает стек до вызова функции.</summary>
internal sealed class PxReturnSignal : Exception
{
    public Values.PxValue? Value { get; }

    public PxReturnSignal(Values.PxValue? value) => Value = value;
}
