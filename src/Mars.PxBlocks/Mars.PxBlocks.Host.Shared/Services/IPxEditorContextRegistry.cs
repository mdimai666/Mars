namespace Mars.PxBlocks.Host.Shared.Services;

/// <summary>
/// Серверный реестр контекстов редактора (PxEditorContext): что доступно редактору
/// и как исполняются программы. Наполняется при старте приложения, читается
/// запросами (api/PxBlocks/Contexts, PxRunRequest.ContextName).
/// </summary>
public interface IPxEditorContextRegistry
{
    IReadOnlyList<PxEditorContext> Contexts { get; }

    /// <summary>Контекст по имени (без учёта регистра); null — не зарегистрирован.</summary>
    PxEditorContext? Get(string name);

    /// <summary>Регистрация контекста; повторное имя — ошибка конфигурации.</summary>
    void Register(PxEditorContext context);
}
