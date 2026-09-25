namespace Mars.CodeCompletion.Host;

public class CodeCompletionOptions
{
    public const string SectionKey = "CodeCompletion";
    public const int DefaultIdleTimeoutMinutes = 30;

    /// <summary>
    /// Минуты без запросов к контексту, после которых его инфраструктура
    /// (workspace, metadata references, type index, MEF-хост) освобождается.
    /// Следующий запрос платит полную цену первого запроса (~секунды).
    /// </summary>
    public int IdleTimeoutMinutes { get; set; } = DefaultIdleTimeoutMinutes;
}
