using Mars.PxBlocks.Host.Shared.Dto;
using Mars.PxBlocks.Runtime.Execution;

namespace Mars.PxBlocks.Host.Shared.Services;

/// <summary>Рассылка событий исполнения подключённым редакторам (SignalR-группа).</summary>
public interface IPxBlocksBroadcaster
{
    Task RunEvents(Guid runId, IReadOnlyList<PxExecutionEvent> events);

    Task RunFinished(Guid runId, PxRunResultDto result);
}
