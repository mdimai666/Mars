namespace Mars.PxBlocks.Runtime.Parsing;

/// <summary>
/// TypeId структурных блоков: парсятся в отдельные AST-узлы и исполняются ядром
/// PxInterpreter. Регистрация IPxBlockImplement для них не нужна и не возможна —
/// это семантика языка (скоупы, break/continue, короткое замыкание).
/// Конвенция typeId: core.категория.имя (определения — PxStandardBlocks/PxEventBlocks),
/// пакеты хостов — пакет.категория.имя. Процедуры — пока Blockly-имена (фаза 2).
/// </summary>
public static class PxCoreBlocks
{
    public const string If = "core.logic.if";
    public const string IfElse = "core.logic.if_else";
    public const string RepeatExt = "core.loops.repeat";
    public const string WhileUntil = "core.loops.while_until";
    public const string For = "core.loops.for";
    public const string ForEach = "core.loops.for_each";
    public const string FlowStatements = "core.loops.flow";

    public const string VariablesGet = "core.variables.get";
    public const string VariablesSet = "core.variables.set";
    public const string VariablesChange = "core.variables.change";

    public const string ProceduresDefNoReturn = "procedures_defnoreturn";
    public const string ProceduresDefReturn = "procedures_defreturn";
    public const string ProceduresCallNoReturn = "procedures_callnoreturn";
    public const string ProceduresCallReturn = "procedures_callreturn";
    public const string IfReturn = "procedures_ifreturn";
    public const string ProceduresReturn = "procedures_return";

    public const string LogicOperation = "core.logic.operation";
    public const string LogicTernary = "core.logic.ternary";
    public const string LogicNull = "core.logic.null";

    /// <summary>Хат-блок «старт» — тело исполняется один раз при запуске (аналог setup()).</summary>
    public const string StartEvent = "core.events.start";

    /// <summary>Хат-блок «цикл» — тело повторяется после старта (аналог loop()).</summary>
    public const string LoopEvent = "core.events.loop";
}
