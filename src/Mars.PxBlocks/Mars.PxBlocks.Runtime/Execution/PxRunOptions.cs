using Mars.PxBlocks.Runtime.Values;

namespace Mars.PxBlocks.Runtime.Execution;

/// <summary>Параметры запуска PxInterpreter.RunAsync.</summary>
public sealed class PxRunOptions
{
    /// <summary>
    /// Максимум шагов (операторов и итераций циклов) — защита от бесконечных циклов.
    /// 0 или отрицательное — без лимита (тогда единственный ограничитель — CancellationToken).
    /// </summary>
    public int StepLimit { get; init; } = 100_000;

    /// <summary>Каждые N шагов — Task.Yield, чтобы UI (WASM) оставался отзывчивым. 0 — не уступать.</summary>
    public int YieldEvery { get; init; } = 1024;

    /// <summary>Зерно генератора случайных чисел — воспроизводимость в тестах.</summary>
    public int? RandomSeed { get; init; }

    /// <summary>
    /// Максимум накопленных строк вывода (PxExecutionResult.Output); 0 или отрицательное —
    /// без лимита. События Output стримятся подписчику независимо от лимита — он защищает
    /// только память при бесконечных событиях Loop на сервере.
    /// </summary>
    public int OutputLimit { get; init; }

    /// <summary>Подписка на события исполнения (подсветка блока, вывод).</summary>
    public Action<PxExecutionEvent>? OnEvent { get; init; }

    /// <summary>
    /// Режим «только переданные события»: исполняются только блоки-события (PxEventBlock),
    /// фазы идут в порядке этого списка — сначала ВСЕ события с первым именем (в порядке
    /// workspace), затем со вторым и т.д. Например, ["start", "loop"] — все «старты»,
    /// затем все «циклы» (Loop гарантированно после Start); пустой список — ничего.
    /// null (по умолчанию) — все верхнеуровневые стеки, события Loop — после всех.
    /// </summary>
    public IReadOnlyList<string>? EventNames { get; init; }

    /// <summary>
    /// Состояние запуска — объект хоста (браузер, соединение, сервис…), доступный
    /// имплементациям: конструктором (имплементации создаются в момент запуска)
    /// или через PxContext.GetState. Запускающий передаёт его на время запуска;
    /// PxRunManager диспозит его по завершении (IDisposable/IAsyncDisposable).
    /// </summary>
    public object? State { get; init; }

    /// <summary>
    /// Начальные значения переменных: имя → значение. Перезаписывают только
    /// переменные, объявленные в workspace; неизвестные имена игнорируются.
    /// </summary>
    public IReadOnlyDictionary<string, PxValue>? InitialVariables { get; init; }
}

/// <summary>Итог исполнения программы.</summary>
public sealed record PxExecutionResult
{
    public required bool Success { get; init; }

    public string? ErrorMessage { get; init; }

    /// <summary>Блок, на котором исполнение упало — подсветить в редакторе.</summary>
    public string? ErrorBlockId { get; init; }

    public bool Canceled { get; init; }

    public long Steps { get; init; }

    public IReadOnlyList<string> Output { get; init; } = [];
}
