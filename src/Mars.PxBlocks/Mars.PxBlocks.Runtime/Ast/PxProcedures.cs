namespace Mars.PxBlocks.Runtime.Ast;

public sealed record PxParam(string Id, string Name);

/// <summary>
/// Определение функции (procedures_defnoreturn / procedures_defreturn). Парсер собирает
/// их в PxProgram.Procedures откуда бы они ни встретились; интерпретатор исполняет при вызове.
/// </summary>
public sealed record PxProcedureDef : PxStatement
{
    public required string Name { get; init; }

    public List<PxParam> Params { get; init; } = [];

    /// <summary>Стек операторов тела (вход STACK).</summary>
    public PxStatement? Body { get; init; }

    /// <summary>Выражение возврата procedures_defreturn (вход RETURN).</summary>
    public PxExpression? Return { get; init; }
}

/// <summary>procedures_callnoreturn — вызов в позиции оператора.</summary>
public sealed record PxProcedureCallStatement : PxStatement
{
    public required string Name { get; init; }

    public List<PxExpression> Args { get; init; } = [];
}

/// <summary>procedures_callreturn — вызов в позиции выражения.</summary>
public sealed record PxProcedureCallExpression : PxExpression
{
    public required string Name { get; init; }

    public List<PxExpression> Args { get; init; } = [];
}

/// <summary>procedures_ifreturn: если условие истинно — вернуть значение из функции.</summary>
public sealed record PxIfReturnStatement : PxStatement
{
    public required PxExpression Condition { get; init; }

    public PxExpression? Value { get; init; }
}
