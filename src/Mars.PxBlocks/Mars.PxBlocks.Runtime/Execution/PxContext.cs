using Mars.PxBlocks.Runtime.Ast;
using Mars.PxBlocks.Runtime.Values;

namespace Mars.PxBlocks.Runtime.Execution;

/// <summary>
/// Контекст исполнения: переменные, процедуры, вывод, события, лимиты.
/// Передаётся имплементациям листьев (IPxBlockImplement). Создаётся интерпретатором.
/// </summary>
public sealed class PxContext
{
    private readonly List<string> _output = [];

    internal PxScope Global { get; }
    internal PxBlockImplementsLocator Implements { get; }
    internal Dictionary<string, PxProcedureDef> Procedures { get; } = new(StringComparer.Ordinal);
    internal Action<PxExecutionEvent>? RaiseEvent { get; set; }

    public CancellationToken CancellationToken { get; }

    public int StepLimit { get; }

    /// <summary>Каждые N шагов — Task.Yield (отзывчивость UI в WASM); 0 — не уступать.</summary>
    public int YieldEvery { get; }

    public long Steps { get; private set; }

    public Random Random { get; }

    /// <summary>Собранные строки вывода (text_print); параллельно стримятся событием Output.</summary>
    public IReadOnlyList<string> OutputLines => _output;

    /// <summary>Отображение id переменной → имя (для сообщений об ошибках).</summary>
    public IReadOnlyDictionary<string, string> VariableNames { get; }

    internal PxContext(
        PxProgram program,
        PxRunOptions options,
        PxBlockImplementsLocator implements,
        CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
        StepLimit = options.StepLimit;
        YieldEvery = options.YieldEvery;
        RaiseEvent = options.OnEvent;
        Implements = implements;
        Random = options.RandomSeed is int seed ? new Random(seed) : new Random();

        Global = new PxScope();
        VariableNames = program.Variables.ToDictionary(v => v.Id, v => v.Name, StringComparer.Ordinal);

        // Переменные workspace объявлены заранее и стартуют с нуля (как в MakeCode).
        foreach (var variable in program.Variables)
            Global.Define(variable.Id, PxNumberValue.Zero);

        foreach (var procedure in program.Procedures)
            Procedures[procedure.Name] = procedure;
    }

    /// <summary>Прочитать переменную по id (текстовым реализациям — text_append и т.п.).</summary>
    public PxValue GetVariable(string variableId) => Global.Get(variableId);

    /// <summary>Записать переменную по id (привязка ищется по цепочке скоупов).</summary>
    public void SetVariable(string variableId, PxValue value) => Global.Set(variableId, value);

    /// <summary>Строка в панель вывода (text_print) + событие Output.</summary>
    public void Print(string text)
    {
        _output.Add(text);
        RaiseEvent?.Invoke(new PxExecutionEvent(PxExecutionEventKind.Output, null, text));
    }

    internal void Fire(PxExecutionEvent executionEvent) => RaiseEvent?.Invoke(executionEvent);

    /// <summary>Шаг исполнения: отмена, лимит шагов (StepLimit &lt;= 0 — без лимита), периодическая уступка потоку.</summary>
    internal async ValueTask StepAsync(string? blockId)
    {
        CancellationToken.ThrowIfCancellationRequested();

        Steps++;
        if (StepLimit > 0 && Steps > StepLimit)
            throw new PxStepLimitExceededException(blockId, StepLimit);

        if (YieldEvery > 0 && Steps % YieldEvery == 0)
            await Task.Yield();
    }
}
