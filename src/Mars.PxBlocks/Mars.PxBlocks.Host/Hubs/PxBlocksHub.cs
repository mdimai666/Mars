using Mars.PxBlocks.Host.Shared;
using Mars.PxBlocks.Host.Shared.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Mars.PxBlocks.Host.Hubs;

/// <summary>
/// Хаб событий исполнения PxBlocks. Подключение автоматически входит в группу
/// рассылки (она одна) — клиенту, в отличие от Mars.Nodes, JoinGroup не нужен.
/// </summary>
public class PxBlocksHub : Hub<IPxBlocksClient>
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, PxBlocksConstants.NotifyGroupName);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, PxBlocksConstants.NotifyGroupName);
        await base.OnDisconnectedAsync(exception);
    }
}
