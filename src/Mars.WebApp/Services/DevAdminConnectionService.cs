using Mars.Core.Models;
using Mars.Host.Shared.Hubs;
using Mars.Host.Shared.Services;
using Microsoft.AspNetCore.SignalR;

namespace Mars.Services;

internal class DevAdminConnectionService : IDevAdminConnectionService
{
    readonly IHubContext<ChatHub> hub;

    public DevAdminConnectionService(IHubContext<ChatHub> hub)
    {
        this.hub = hub;
    }

    public Task ShowNotifyMessage(string message, MessageIntent? messageIntent = MessageIntent.Info)
    {
        return hub.Clients.All.SendAsync("ShowNotifyMessage", message, messageIntent);
    }
}
