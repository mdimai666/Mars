namespace Mars.Contracts.Hubs;

/// <summary>
/// Имена событий, которые сервер отправляет админ-клиенту через ChatHub (/_ws/admin).
/// </summary>
public static class AdminHubEvents
{
    /// <summary>
    /// Список постов изменился (создан/обновлён/удалён). Payload: имя типа поста (string).
    /// </summary>
    public const string PostListChanged = "PostListChanged";
}
