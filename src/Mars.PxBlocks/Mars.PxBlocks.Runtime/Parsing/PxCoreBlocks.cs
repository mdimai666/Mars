namespace Mars.PxBlocks.Runtime.Parsing;

/// <summary>
/// TypeId структурных блоков: парсятся в отдельные AST-узлы и исполняются ядром
/// PxInterpreter. Регистрация IPxBlockImplement для них не нужна и не возможна —
/// это семантика языка (скоупы, break/continue, короткое замыкание).
/// </summary>
public static class PxCoreBlocks
{
    public const string If = "controls_if";
    public const string IfElse = "controls_if_else";
    public const string RepeatExt = "controls_repeat_ext";
    public const string WhileUntil = "controls_whileUntil";
    public const string For = "controls_for";
    public const string ForEach = "controls_forEach";
    public const string FlowStatements = "controls_flow_statements";

    public const string VariablesGet = "variables_get";
    public const string VariablesSet = "variables_set";

    public const string ProceduresDefNoReturn = "procedures_defnoreturn";
    public const string ProceduresDefReturn = "procedures_defreturn";
    public const string ProceduresCallNoReturn = "procedures_callnoreturn";
    public const string ProceduresCallReturn = "procedures_callreturn";
    public const string IfReturn = "procedures_ifreturn";

    public const string LogicOperation = "logic_operation";
    public const string LogicTernary = "logic_ternary";
    public const string LogicNull = "logic_null";

    /// <summary>Хат-блок «старт» — тело исполняется один раз при запуске (аналог setup()).</summary>
    public const string StartEvent = "px_start";

    /// <summary>Хат-блок «цикл» — тело повторяется после старта (аналог loop()).</summary>
    public const string LoopEvent = "px_loop";
}
