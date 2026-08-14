using Mars.PxBlocks.Host.Shared.Dto;
using Mars.PxBlocks.Runtime.Execution;

namespace Mars.PxBlocks.Host.Shared.Hubs;

/// <summary>Контракт типизированного хаба PxBlocks: что сервер шлёт подключённым редакторам.</summary>
public interface IPxBlocksClient
{
    /// <summary>Пакет событий исполнения запуска runId (подсветка/вывод).</summary>
    Task RunEvents(Guid runId, IReadOnlyList<PxExecutionEvent> events);

    /// <summary>Запуск runId завершён (успех/ошибка/остановка).</summary>
    Task RunFinished(Guid runId, PxRunResultDto result);
}
