using Mars.PxBlocks.Host.Shared.Dto;
using Mars.PxBlocks.Runtime.Execution;

namespace Mars.PxBlocks.Host.Shared.Services;

/// <summary>
/// Транспорт исполнения для редактора: Run/Stop через сервер + подписка на события
/// конкретного запуска. Реализация — PxServerRunClient в Mars.PxBlocks.Workspace.
/// RunId генерирует клиент и подписывается ДО запроса Run — события не теряются.
/// </summary>
public interface IPxRunTransport
{
    /// <summary>Подключиться к хабу событий (идемпотентно). Вызывать до RunAsync.</summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    Task<PxRunResponse> RunAsync(PxRunRequest request, CancellationToken cancellationToken = default);

    /// <summary>false — запуск серверу неизвестен (завершён или не стартовал).</summary>
    Task<bool> StopAsync(Guid runId, CancellationToken cancellationToken = default);

    /// <summary>Регистрация обработчиков запуска runId; IDisposable снимает её.</summary>
    IDisposable Subscribe(
        Guid runId,
        Action<IReadOnlyList<PxExecutionEvent>> onEvents,
        Action<PxRunResultDto> onFinished);
}
