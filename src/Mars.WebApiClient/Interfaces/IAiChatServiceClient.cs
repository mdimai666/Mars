using Mars.AiChat.Shared.Dto;

namespace Mars.WebApiClient.Interfaces;

public interface IAiChatServiceClient
{
    Task<IReadOnlyList<AiChatSessionSummary>> GetSessions();
    Task<AiChatSessionSummary> CreateSession(string? title = null, string? connectionName = null);
    Task<AiChatSessionDto> GetSession(Guid chatId);
    Task DeleteSession(Guid chatId);

    /// <summary>Настроенные подключения к ИИ-сервисам (без секретов).</summary>
    Task<IReadOnlyList<AiChatConnectionDto>> GetConnections();

    /// <summary>Выбирает подключение (модель) для чата; пустое имя — вернуть дефолт.</summary>
    Task<AiChatSessionDto> SetConnection(Guid chatId, string? connectionName);

    /// <summary>
    /// Загружает файл-вложение чата в медиатеку (POST api/AiChat/attachments).
    /// </summary>
    Task<AiChatAttachmentDto> UploadAttachment(Stream fileStream, string fileName);

    /// <summary>
    /// Отправляет сообщение агенту (202). Ответ приходит событиями SignalR.
    /// </summary>
    Task Send(Guid chatId, string message, string? pageContext = null, IReadOnlyList<Guid>? attachmentIds = null);

    Task<bool> Stop(Guid chatId);
}
