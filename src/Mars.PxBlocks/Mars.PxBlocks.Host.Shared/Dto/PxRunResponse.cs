namespace Mars.PxBlocks.Host.Shared.Dto;

/// <summary>
/// Ответ на запуск. Разбор программы выполняется синхронно: ошибка разбора —
/// Started=false + ErrorMessage/ErrorBlockId (запуск не начинается); иначе
/// Started=true + RunId, события исполнения приходят через хаб.
/// </summary>
public sealed record PxRunResponse
{
    public Guid RunId { get; init; }

    public bool Started { get; init; }

    /// <summary>Ошибка разбора (неизвестный блок и т.п.) — запуск не состоялся.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Блок, в котором найдена ошибка, — подсветить в редакторе.</summary>
    public string? ErrorBlockId { get; init; }
}
