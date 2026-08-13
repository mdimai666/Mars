namespace Mars.AiChat.Shared.Dto;

public class AiChatSessionSummary
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ModifiedAtUtc { get; set; }
    public bool IsRunning { get; set; }

    /// <summary>
    /// Последний вопрос агента, ожидающий ответа пользователя.
    /// </summary>
    public string? PendingQuestion { get; set; }
}

/// <summary>
/// Чат с историей сообщений.
/// </summary>
public class AiChatSessionDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public bool IsRunning { get; set; }
    public string? PendingQuestion { get; set; }

    /// <summary>Выбранное подключение (имя); null/пусто — подключение по умолчанию.</summary>
    public string? ConnectionName { get; set; }

    public List<AiChatMessageDto> Messages { get; set; } = [];
}
