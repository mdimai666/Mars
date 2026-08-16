using System.Text.Json.Nodes;

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
    /// Имя контекста редактора (IPxEditorContextRegistry): политика запуска и лимиты
    /// берутся из контекста, явно заданные поля запроса имеют приоритет.
    /// null — запуск вне контекста.
    /// </summary>
    public string? ContextName { get; init; }

    /// <summary>
    /// Режим «только события»: фазы в порядке списка имён (PxEvents.Start / PxEvents.Loop).
    /// null — все верхнеуровневые стеки в порядке workspace, события Loop — после всех
    /// (либо политика контекста, если задан <see cref="ContextName"/>).
    /// </summary>
    public IReadOnlyList<string>? EventNames { get; init; }

    /// <summary>
    /// Лимит шагов; 0 или отрицательное — без лимита (остановка только через Stop).
    /// null — лимит контекста (<see cref="ContextName"/>), иначе без лимита.
    /// </summary>
    public int? StepLimit { get; init; }

    /// <summary>
    /// Максимум накопленных строк вывода в итоге; 0 — без лимита.
    /// null — лимит контекста (<see cref="ContextName"/>), иначе 10 000.
    /// </summary>
    public int? OutputLimit { get; init; }

    /// <summary>Зерно генератора случайных чисел — воспроизводимость.</summary>
    public int? RandomSeed { get; init; }

    /// <summary>
    /// Начальные значения переменных: имя переменной → JSON-значение
    /// (число/строка/булево/объект/массив/null). Перезаписывают только переменные,
    /// объявленные в workspace; неизвестные имена игнорируются.
    /// </summary>
    public IReadOnlyDictionary<string, JsonNode?>? InitialVariables { get; init; }
}
