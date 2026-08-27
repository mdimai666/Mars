namespace Mars.AiChat.Contracts.SignalR;

/// <summary>
/// Имена событий SignalR хаба /_ws/aichat (сервер -> клиент).
/// Клиент -> сервер: JoinChat(chatId), LeaveChat(chatId), PageToolResult(chatId, result).
/// </summary>
public static class AiChatHubEvents
{
    public const string HubPath = "/_ws/aichat";

    /// <summary>Фрагмент текста ответа: (Guid chatId, Guid runId, string text)</summary>
    public const string Chunk = "AiChatChunk";

    /// <summary>Вызов инструмента: (Guid chatId, Guid runId, string toolName, string argumentsJson)</summary>
    public const string ToolCall = "AiChatToolCall";

    /// <summary>Результат инструмента: (Guid chatId, Guid runId, string toolName, string result)</summary>
    public const string ToolResult = "AiChatToolResult";

    /// <summary>Агент задал вопрос пользователю: (Guid chatId, Guid runId, string question)</summary>
    public const string Question = "AiChatQuestion";

    /// <summary>Запуск завершён: (Guid chatId, Guid runId, string finalText)</summary>
    public const string Done = "AiChatDone";

    /// <summary>Запуск остановлен пользователем: (Guid chatId, Guid runId)</summary>
    public const string Stopped = "AiChatStopped";

    /// <summary>Ошибка запуска: (Guid chatId, Guid runId, string message)</summary>
    public const string Error = "AiChatError";

    /// <summary>Выполнить инструмент на открытой странице клиента: (Guid chatId, AiPageToolRequest request)</summary>
    public const string PageToolRequest = "AiChatPageToolRequest";
}
