using Mars.Core.Models;
using Mars.Host.Shared.Hubs;
using Mars.Host.Shared.Services;
using Mars.Shared.Tools;
using Microsoft.AspNetCore.SignalR;

namespace Mars.Services;

internal class DevAdminConnectionService : IDevAdminConnectionService
{
    readonly IHubContext<ChatHub> hub;
    private readonly ModelInfoService _modelInfoService;

    public DevAdminConnectionService(IHubContext<ChatHub> hub, ModelInfoService modelInfoService)
    {
        this.hub = hub;
        _modelInfoService = modelInfoService;
    }

    public Task ShowNotifyMessage(string message, string userId, MessageIntent? messageIntent = MessageIntent.Info)
    {
        return hub.Clients.User(userId).SendAsync("ShowNotifyMessage", message, messageIntent);
    }

    public Task ShowNotifyMessageForAll(string message, MessageIntent? messageIntent = MessageIntent.Info)
    {
        return hub.Clients.All.SendAsync("ShowNotifyMessage", message, messageIntent);
    }

    public IReadOnlyCollection<PageContextInfo> GetPageContexts()
    {
        var pages = _modelInfoService.GetPagesPageNonId(typeof(AppAdmin.Program).Assembly);

        return pages.Select(x => new PageContextInfo(x.PageType.FullName!, x.DisplayAttributeName ?? x.Name)).ToList();
    }
}
