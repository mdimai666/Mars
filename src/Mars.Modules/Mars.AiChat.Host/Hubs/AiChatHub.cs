using Microsoft.AspNetCore.SignalR;

namespace Mars.AiChat.Host.Hubs;

/// <summary>
/// Хаб ИИ-чата. Сервер пушит события в группу чата (AiChatHubEvents.*),
/// клиент подписывается через JoinChat.
///
/// Info: без [Authorize] — по аналогии с ChatHub. Авторизация WebSocket-рукопожатия
/// через access_token в query не поддерживается "smart"-схемой аутентификации Mars.
/// Данные защищены на уровне REST API (Admin-роль) и в хранилище (изоляция по userId);
/// chatId — неугадываемый Guid.
/// </summary>
public class AiChatHub : Hub
{
    public static string GroupName(Guid chatId) => $"aichat-{chatId}";

    public Task JoinChat(Guid chatId)
        => Groups.AddToGroupAsync(Context.ConnectionId, GroupName(chatId));

    public Task LeaveChat(Guid chatId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(chatId));
}
