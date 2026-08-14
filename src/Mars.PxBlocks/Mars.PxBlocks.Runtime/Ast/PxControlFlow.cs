namespace Mars.PxBlocks.Runtime.Ast;

/// <summary>Ветка if / else-if: условие + тело.</summary>
public sealed record PxIfBranch(PxExpression Condition, PxStatement? Body);

/// <summary>controls_if / controls_if_else.</summary>
public sealed record PxIfStatement : PxStatement
{
    public List<PxIfBranch> Branches { get; init; } = [];

    public PxStatement? ElseBody { get; init; }
}

/// <summary>controls_repeat_ext: повторить N раз.</summary>
public sealed record PxRepeatStatement : PxStatement
{
    public required PxExpression Times { get; init; }

    public PxStatement? Body { get; init; }
}

public enum PxWhileMode
{
    While,
    Until
}

/// <summary>controls_whileUntil: пока / до тех пор.</summary>
public sealed record PxWhileUntilStatement : PxStatement
{
    public PxWhileMode Mode { get; init; } = PxWhileMode.While;

    public required PxExpression Condition { get; init; }

    public PxStatement? Body { get; init; }
}

/// <summary>controls_for: счётчик от…до…с шагом…</summary>
public sealed record PxForStatement : PxStatement
{
    public required string VariableId { get; init; }

    public required PxExpression From { get; init; }

    public required PxExpression To { get; init; }

    public required PxExpression By { get; init; }

    public PxStatement? Body { get; init; }
}

/// <summary>controls_forEach: для каждого элемента списка.</summary>
public sealed record PxForEachStatement : PxStatement
{
    public required string VariableId { get; init; }

    public required PxExpression List { get; init; }

    public PxStatement? Body { get; init; }
}

public enum PxFlowKind
{
    Break,
    Continue
}

/// <summary>controls_flow_statements: выйти из цикла / продолжить.</summary>
public sealed record PxFlowStatement : PxStatement
{
    public PxFlowKind Kind { get; init; }
}
