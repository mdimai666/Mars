namespace Mars.PxBlocks.Host.Shared.Dto;

/// <summary>Запрос на серверное исполнение PxBlocks-программы (POST api/PxBlocks/Run).</summary>
public sealed record PxRunRequest
{
    /// <summary>
    /// Id запуска, назначенный клиентом: клиент подписывается на события хаба до
    /// запроса Run, поэтому события не теряются. Empty — сервер назначит сам.
    /// </summary>
    public Guid RunId { get; init; }

    /// <summary>Сохранённый workspace — нативный Blockly serialization JSON.</summary>
    public required string BlocksJson { get; init; }

    /// <summary>
    /// Режим «только события»: фазы в порядке списка имён (PxEvents.Start / PxEvents.Loop).
    /// null — все верхнеуровневые стеки в порядке workspace, события Loop — после всех.
    /// </summary>
    public IReadOnlyList<string>? EventNames { get; init; }

    /// <summary>Лимит шагов; 0 или отрицательное — без лимита (остановка только через Stop).</summary>
    public int StepLimit { get; init; }

    /// <summary>Максимум накопленных строк вывода в итоге; 0 — без лимита.</summary>
    public int OutputLimit { get; init; } = 10_000;

    /// <summary>Зерно генератора случайных чисел — воспроизводимость.</summary>
    public int? RandomSeed { get; init; }
}
