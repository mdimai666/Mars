namespace Mars.AiChat.Shared.Dto;

public enum AiChatMessageRole
{
    User = 0,
    Assistant = 1,
    Tool = 2,
    Error = 3,
    Info = 4,
}

/// <summary>
/// Сообщение чата для отображения в терминале.
/// </summary>
public class AiChatMessageDto
{
    public Guid Id { get; set; }
    public AiChatMessageRole Role { get; set; }
    public string Content { get; set; } = "";

    /// <summary>
    /// Имя инструмента для сообщений с ролью Tool.
    /// </summary>
    public string? ToolName { get; set; }

    /// <summary>
    /// Для роли Tool: true — результат инструмента, false — вызов инструмента (в Content аргументы JSON).
    /// </summary>
    public bool IsToolResult { get; set; }

    /// <summary>
    /// Приложенные медиафайлы (для сообщений пользователя).
    /// </summary>
    public List<AiChatAttachmentDto>? Attachments { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
