using Mars.PxBlocks.Host.Hubs;
using Mars.PxBlocks.Host.Shared;
using Mars.PxBlocks.Host.Shared.Dto;
using Mars.PxBlocks.Host.Shared.Hubs;
using Mars.PxBlocks.Host.Shared.Services;
using Mars.PxBlocks.Runtime.Execution;
using Microsoft.AspNetCore.SignalR;

namespace Mars.PxBlocks.Host.Services;

/// <summary>Рассылка событий исполнения в SignalR-группу подключённых редакторов.</summary>
public sealed class PxBlocksBroadcaster(IHubContext<PxBlocksHub, IPxBlocksClient> hubContext) : IPxBlocksBroadcaster
{
    private IPxBlocksClient Clients => hubContext.Clients.Group(PxBlocksConstants.NotifyGroupName);

    public Task RunEvents(Guid runId, IReadOnlyList<PxExecutionEvent> events)
        => Clients.RunEvents(runId, events);

    public Task RunFinished(Guid runId, PxRunResultDto result)
        => Clients.RunFinished(runId, result);
}
