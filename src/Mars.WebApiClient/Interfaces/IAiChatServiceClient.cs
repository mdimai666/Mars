using Mars.AiChat.Shared.Dto;

namespace Mars.WebApiClient.Interfaces;

public interface IAiChatServiceClient
{
    Task<IReadOnlyList<AiChatSessionSummary>> GetSessions();
    Task<AiChatSessionSummary> CreateSession(string? title = null);
    Task<AiChatSessionDto> GetSession(Guid chatId);
    Task DeleteSession(Guid chatId);

    /// <summary>
    /// Отправляет сообщение агенту (202). Ответ приходит событиями SignalR.
    /// </summary>
    Task Send(Guid chatId, string message);

    Task<bool> Stop(Guid chatId);
}
