using Mars.AiChat.Contracts.Dto;
using Mars.AiChat.Host.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Mars.AiChat.Host.Hubs;

/// <summary>
/// Хаб ИИ-чата. Сервер пушит события в группу чата (AiChatHubEvents.*),
/// клиент подписывается через JoinChat.
///
/// Info: [Authorize] работает на cookie-схеме (A1): браузер шлёт Identity-cookie
/// и в WS-рукопожатии, и в negotiate. Данные дополнительно защищены на уровне
/// REST API (Admin-роль) и в хранилище (изоляция по userId).
/// </summary>
[Authorize]
public class AiChatHub : Hub
{
    private readonly AiChatPageBridge _pageBridge;

    public AiChatHub(AiChatPageBridge pageBridge)
    {
        _pageBridge = pageBridge;
    }

    public static string GroupName(Guid chatId) => $"aichat-{chatId}";

    public Task JoinChat(Guid chatId)
        => Groups.AddToGroupAsync(Context.ConnectionId, GroupName(chatId));

    public Task LeaveChat(Guid chatId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(chatId));

    /// <summary>
    /// Результат инструмента, выполненного клиентом на открытой странице.
    /// </summary>
    public void PageToolResult(Guid chatId, AiPageToolResult result)
    {
        _pageBridge.Complete(result);
    }
}
