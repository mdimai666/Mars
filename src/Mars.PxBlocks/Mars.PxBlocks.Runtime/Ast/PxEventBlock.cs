namespace Mars.PxBlocks.Runtime.Ast;

/// <summary>
/// Блок-событие (хат-блок): Start и Loop — аналоги setup()/loop() из Arduino.
/// Не подключается к стекам (нет prev/next), исполнение зависит от режима запуска:
/// все события Start/обычные стеки идут в порядке workspace, события Loop — после всех.
/// </summary>
public sealed record PxEventBlock : PxStatement
{
    /// <summary>Имя события (<see cref="PxEvents"/>).</summary>
    public required string EventName { get; init; }

    /// <summary>Тело события (вход DO).</summary>
    public PxStatement? Body { get; init; }
}

/// <summary>Имена событий — для фильтра режима запуска «только переданные» (PxRunOptions.EventNames).</summary>
public static class PxEvents
{
    public const string Start = "start";

    public const string Loop = "loop";
}
