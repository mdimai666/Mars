using AppFront.Shared.Interfaces;
using Mars.Host.Shared.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.Shared.Hubs;

/// <summary>
/// Хаб только получает сообщение  от клиента и вызывает метод в сервисе
/// </summary>
public class ChatHub : Hub<IClientHub>
{
    private readonly INodeService _nodeService;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public ChatHub(INodeService nodeService, IServiceScopeFactory serviceScopeFactory)
    {
        _nodeService = nodeService;
        _serviceScopeFactory = serviceScopeFactory;
    }

    //public async Task SendMessage(string user, string message)
    //{
    //    Console.WriteLine($">SendMessage= {user}; message={message}");
    //    await Clients.Others.SendAsync("ReceiveMessage", user, message);
    //}

    public async void Inject(string nodeId)
    {
        //await nodeService.Inject(factory, nodeId);
        await _nodeService.InjectAsync(_serviceScopeFactory, nodeId);
    }

    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        // Опционально: оповестить других в группе
        //await Clients.Group(groupName).SendAsync("Notify", $"{Context.ConnectionId} joined {groupName}");
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        // Опционально: оповестить других в группе
        //await Clients.Group(groupName).SendAsync("Notify", $"{Context.ConnectionId} joined {groupName}");
    }

}
