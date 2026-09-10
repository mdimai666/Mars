using Mars.AiChat.Contracts.Dto;

namespace Mars.AiChat.Abstractions.Models;

/// <summary>
/// Серверное состояние чата. Хранится в HybridCache.
/// </summary>
public class AiChatSessionState
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ModifiedAtUtc { get; set; }

    /// <summary>
    /// Сериализованная AgentSession (история диалога с точки зрения агента).
    /// </summary>
    public string? SerializedAgentSession { get; set; }

    /// <summary>
    /// Последний вопрос агента, ожидающий ответа пользователя.
    /// </summary>
    public string? PendingQuestion { get; set; }

    /// <summary>
    /// Выбранное для чата подключение (имя из AiChatOption.Connections).
    /// null/пусто — подключение по умолчанию.
    /// </summary>
    public string? ConnectionName { get; set; }

    public List<AiChatMessageDto> Messages { get; set; } = [];

    public AiChatSessionSummary ToSummary(bool isRunning) => new()
    {
        Id = Id,
        Title = Title,
        CreatedAtUtc = CreatedAtUtc,
        ModifiedAtUtc = ModifiedAtUtc,
        IsRunning = isRunning,
        PendingQuestion = PendingQuestion,
    };

    public AiChatSessionDto ToDto(bool isRunning) => new()
    {
        Id = Id,
        Title = Title,
        CreatedAtUtc = CreatedAtUtc,
        IsRunning = isRunning,
        PendingQuestion = PendingQuestion,
        ConnectionName = ConnectionName,
        Messages = Messages,
    };
}
