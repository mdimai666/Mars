using Mars.PxBlocks.Runtime.Ast;
using Mars.PxBlocks.Runtime.Values;

namespace Mars.PxBlocks.Runtime.Execution;

/// <summary>
/// Контекст исполнения: переменные, процедуры, вывод, события, лимиты, состояние
/// запуска и имплементации запуска. Передаётся имплементациям листьев
/// (IPxBlockImplement). Создаётся интерпретатором.
/// </summary>
public sealed class PxContext
{
    private readonly List<string> _output = [];
    private readonly Dictionary<string, IPxBlockImplement> _instances = [];

    internal PxScope Global { get; }
    internal PxBlockImplementsLocator Implements { get; }
    internal Dictionary<string, PxProcedureDef> Procedures { get; } = new(StringComparer.Ordinal);
    internal Action<PxExecutionEvent>? RaiseEvent { get; set; }

    public CancellationToken CancellationToken { get; }

    public int StepLimit { get; }

    /// <summary>Каждые N шагов — Task.Yield (отзывчивость UI в WASM); 0 — не уступать.</summary>
    public int YieldEvery { get; }

    /// <summary>Максимум накопленных строк вывода (OutputLines); 0 — без лимита.</summary>
    public int OutputLimit { get; }

    public long Steps { get; private set; }

    public Random Random { get; }

    /// <summary>Собранные строки вывода (text_print); параллельно стримятся событием Output.</summary>
    public IReadOnlyList<string> OutputLines => _output;

    /// <summary>Отображение id переменной → имя (для сообщений об ошибках).</summary>
    public IReadOnlyDictionary<string, string> VariableNames { get; }

    /// <summary>
    /// Состояние запуска — объект, который хост передал на исполнение (браузер,
    /// соединение, сервис…). Имплементации получают его конструктором (создаются
    /// в момент запуска) или через <see cref="GetState{T}"/>.
    /// </summary>
    public object? State { get; }

    internal PxContext(
        PxProgram program,
        PxRunOptions options,
        PxBlockImplementsLocator implements,
        object? state,
        CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
        StepLimit = options.StepLimit;
        YieldEvery = options.YieldEvery;
        OutputLimit = options.OutputLimit;
        RaiseEvent = options.OnEvent;
        Implements = implements;
        State = state;
        Random = options.RandomSeed is int seed ? new Random(seed) : new Random();

        Global = new PxScope();
        VariableNames = program.Variables.ToDictionary(v => v.Id, v => v.Name, StringComparer.Ordinal);

        // Переменные workspace объявлены заранее: стартовые значения — по именам
        // из InitialVariables, остальные — с нуля (как в MakeCode).
        foreach (var variable in program.Variables)
        {
            var initial = options.InitialVariables != null
                && options.InitialVariables.TryGetValue(variable.Name, out var initialVariable)
                    ? initialVariable
                    : PxNumberValue.Zero;
            Global.Define(variable.Id, initial);
        }

        foreach (var procedure in program.Procedures)
            Procedures[procedure.Name] = procedure;
    }

    /// <summary>Состояние запуска в типе домена; отсутствует/другой тип — ошибка исполнения.</summary>
    public T GetState<T>() => State is T typed
        ? typed
        : throw new PxRuntimeException($"Состояние запуска не задано или не является '{typeof(T).Name}'");

    /// <summary>
    /// Имплементация блока для этого запуска: экземпляр создаётся при первом
    /// обращении (с инъекцией состояния запуска) и живёт до конца запуска.
    /// null — тип не зарегистрирован.
    /// </summary>
    public IPxBlockImplement? Implement(string typeId)
    {
        if (_instances.TryGetValue(typeId, out var instance))
            return instance;

        if (!Implements.Knows(typeId))
            return null;

        instance = Implements.Create(typeId, State);
        _instances[typeId] = instance;
        return instance;
    }

    /// <summary>Прочитать переменную по id (текстовым реализациям — text_append и т.п.).</summary>
    public PxValue GetVariable(string variableId) => Global.Get(variableId);

    /// <summary>Записать переменную по id (привязка ищется по цепочке скоупов).</summary>
    public void SetVariable(string variableId, PxValue value) => Global.Set(variableId, value);

    /// <summary>Строка в панель вывода (text_print) + событие Output.</summary>
    public void Print(string text)
    {
        if (OutputLimit <= 0 || _output.Count < OutputLimit)
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
