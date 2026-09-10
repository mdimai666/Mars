using Mars.PxBlocks.Host.Hubs;
using Mars.PxBlocks.Contracts;
using Mars.PxBlocks.Contracts.Dto;
using Mars.PxBlocks.Contracts.Hubs;
using Mars.PxBlocks.Contracts.Services;
using Mars.PxBlocks.Runtime.Execution;
using Microsoft.AspNetCore.SignalR;
using Mars.PxBlocks.Abstractions.Services;

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
