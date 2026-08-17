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

/// <summary>procedures_return: досрочный выход из функции (значение необязательно).</summary>
public sealed record PxReturnStatement : PxStatement
{
    public PxExpression? Value { get; init; }
}

public sealed record PxFunctionParam(string Id, string Name, string Type);

/// <summary>
/// function_definition (редактор функций MakeCode, Этап 14C). Парсер собирает их
/// в PxProgram.Functions; интерпретатор исполняет при вызове. Параметры типизированы
/// (number/string/boolean/Array) — тип даёт значение по умолчанию при нехватке аргументов.
/// </summary>
public sealed record PxFunctionDef : PxStatement
{
    public required string Name { get; init; }

    public string FunctionId { get; init; } = "";

    public List<PxFunctionParam> Params { get; init; } = [];

    /// <summary>Стек операторов тела (вход STACK).</summary>
    public PxStatement? Body { get; init; }
}

/// <summary>function_call — вызов в позиции оператора; входы именуются id аргументов.</summary>
public sealed record PxFunctionCallStatement : PxStatement
{
    public required string Name { get; init; }

    public List<PxExpression> Args { get; init; } = [];
}

/// <summary>function_call_output — вызов в позиции выражения.</summary>
public sealed record PxFunctionCallExpression : PxExpression
{
    public required string Name { get; init; }

    public List<PxExpression> Args { get; init; } = [];
}

/// <summary>argument_reporter_* — чтение параметра функции по имени внутри тела.</summary>
public sealed record PxArgumentReporter : PxExpression
{
    public required string ParamName { get; init; }
}
