using System.ComponentModel.DataAnnotations;

namespace Mars.AiChat.Shared.Options;

/// <summary>
/// Настройки ИИ-чата: подключения к ИИ-сервисам и поведение агента.
/// </summary>
public sealed class AiChatOption
{
    [Display(Name = "Подключения к ИИ-сервисам")]
    public List<AiProviderConnection> Connections { get; set; } = [];

    /// <summary>
    /// Name подключения, используемого по умолчанию.
    /// </summary>
    [Display(Name = "Подключение по умолчанию")]
    public string DefaultConnectionName { get; set; } = "";

    /// <summary>
    /// Дополнительные инструкции агенту (добавляются к системному промпту).
    /// </summary>
    [Display(Name = "Дополнительные инструкции агенту")]
    public string Instructions { get; set; } = "";

    /// <summary>
    /// Разрешить агенту доступ к SQL-базам: основная БД Mars (slug "default")
    /// и настроенные data sources — схема, чтение и запись.
    /// </summary>
    [Display(Name = "Разрешить агенту доступ к SQL-базам")]
    public bool EnableSqlAccess { get; set; } = true;

    public AiProviderConnection? GetDefaultConnection()
    {
        if (Connections.Count == 0) return null;

        return Connections.FirstOrDefault(c => c.Name == DefaultConnectionName) ?? Connections[0];
    }
}
