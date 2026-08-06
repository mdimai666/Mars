using Mars.AiChat.Host.Shared.Models;
using Mars.AiChat.Shared.Dto;
using Mars.AiChat.Shared.Options;
using Microsoft.Extensions.AI;

namespace Mars.AiChat.Host.Shared.Interfaces;

/// <summary>
/// Хранилище чатов (HybridCache).
/// </summary>
public interface IAiChatSessionStore
{
    Task<AiChatSessionState?> GetAsync(Guid chatId, Guid userId, CancellationToken ct = default);
    Task SaveAsync(AiChatSessionState state, CancellationToken ct = default);
    Task DeleteAsync(Guid chatId, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<AiChatSessionSummary>> ListAsync(Guid userId, CancellationToken ct = default);
}

/// <summary>
/// Фабрика ИИ-клиентов по настройке подключения.
/// </summary>
public interface IAiChatClientFactory
{
    IChatClient CreateClient(AiProviderConnection connection);
}

/// <summary>
/// Координатор запусков агента: один активный запуск на чат.
/// </summary>
public interface IAiChatRunCoordinator
{
    bool IsRunning(Guid chatId);

    /// <summary>
    /// Ставит сообщение в обработку. Бросает исключение, если чат уже обрабатывается.
    /// </summary>
    void Enqueue(Guid chatId, Guid userId, string userMessage, string? pageContext = null);

    /// <summary>
    /// Останавливает активный запуск чата. Возвращает false, если запуска нет.
    /// </summary>
    bool Stop(Guid chatId);
}
