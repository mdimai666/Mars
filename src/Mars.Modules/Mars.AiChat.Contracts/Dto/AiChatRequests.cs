namespace Mars.AiChat.Shared.Dto;

public class AiChatSendRequest
{
    public string Message { get; set; } = "";

    /// <summary>
    /// Контекст текущей страницы админки (URL), если открыта страница редактирования.
    /// </summary>
    public string? PageContext { get; set; }

    /// <summary>
    /// Идентификаторы медиафайлов, заранее загруженных через POST api/AiChat/attachments.
    /// </summary>
    public IReadOnlyList<Guid>? AttachmentIds { get; set; }
}

public class AiChatCreateSessionRequest
{
    public string? Title { get; set; }

    /// <summary>Подключение (модель) для нового чата; пусто — подключение по умолчанию.</summary>
    public string? ConnectionName { get; set; }
}

/// <summary>
/// Выбор подключения (модели) для чата. Пустое имя — подключение по умолчанию.
/// </summary>
public class AiChatSetConnectionRequest
{
    public string? ConnectionName { get; set; }
}
