using Mars.PxBlocks.Host.Shared.Dto;

namespace Mars.PxBlocks.Host.Shared.Services;

/// <summary>
/// Оркестратор серверных запусков PxBlocks-программ: синхронный разбор,
/// фоновое исполнение, пакеты событий в хаб, остановка по RunId.
/// </summary>
public interface IPxRunManager
{
    /// <summary>
    /// Разобрать и запустить программу. Ошибка разбора — Started=false.
    /// state — объект хоста на время запуска (состояние запуска: браузер, соединение…);
    /// передаётся во владение менеджера и диспозится по завершении, если он
    /// IDisposable/IAsyncDisposable (при Started=false — сразу).
    /// </summary>
    PxRunResponse Start(PxRunRequest request, object? state = null);

    /// <summary>Остановить активный запуск (CancellationToken интерпретатора). false — запуск не найден/уже завершён.</summary>
    bool Stop(Guid runId);

    /// <summary>Число активных запусков.</summary>
    int ActiveRunCount { get; }
}
