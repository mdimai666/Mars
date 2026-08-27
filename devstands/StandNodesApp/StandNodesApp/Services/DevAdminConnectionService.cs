using Mars.Admin.Framework.Interfaces;
using Mars.Core.Models;
using Mars.Nodes.Abstractions.Hubs;
using Mars.Server.Abstractions.Services;
using Mars.SiteEngine.Abstractions.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace StandNodesApp.Services;

internal class DevAdminConnectionService : IDevAdminConnectionService
{
    readonly IHubContext<ChatHub> _hub;
    private readonly IBlazorPagesService _pagesService;

    public DevAdminConnectionService(IHubContext<ChatHub> hub, IBlazorPagesService pagesService)
    {
        _hub = hub;
        _pagesService = pagesService;
    }

    public Task ShowNotifyMessage(string message, string userId, MessageIntent? messageIntent = MessageIntent.Info)
    {
        return _hub.Clients.User(userId).SendAsync("ShowNotifyMessage", message, messageIntent);
    }

    public Task ShowNotifyMessageForAll(string message, MessageIntent? messageIntent = MessageIntent.Info)
    {
        return _hub.Clients.All.SendAsync("ShowNotifyMessage", message, messageIntent);
    }

    public IReadOnlyCollection<PageContextInfo> GetPageContexts()
    {
        var pages = _pagesService.GetStaticRoutedPages([typeof(StandNodesApp.Client._Imports).Assembly]);

        return pages.Select(x => new PageContextInfo(x.PageType.FullName!, x.DisplayName)).ToList();
    }
}
