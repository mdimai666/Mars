using Mars.XActions.Contracts;

namespace Mars.XActions.Abstractions.Managers;

public interface IActionManager
{
    Task RefreshDict(bool force = false);

    /// <summary>
    /// Императивная регистрация команды со всеми метаданными и привязкой исполнения
    /// (Handler&lt;TAct&gt; или Link) — аналог VS Code registerCommand + contributes.commands.
    /// </summary>
    void Add(Action<XActionBuilder> configure);

    /// <summary>
    /// Дозаявить фронтовые контексты для уже зарегистрированной команды:
    /// команду регистрирует доменный владелец без привязки к админке,
    /// контексты навешивает сторона, знающая страницы админ-панели.
    /// </summary>
    void AddFrontContexts(string id, params string[] frontContexts);

    void AddActionsProvider(IXActionCommandsProvider actionCommandsProvider);
    IReadOnlyDictionary<string, XActionCommand> XActions { get; }
    Task<XActResult> Inject(string id, IReadOnlyDictionary<string, string> args, CancellationToken cancellationToken);

    /// <summary>
    /// Регистрация динамического источника вариантов выбора для аргументов команд
    /// (фронт запрашивает их перед отрисовкой формы).
    /// </summary>
    void AddOptionsSource(string key, Func<CancellationToken, Task<IReadOnlyCollection<XActionOption>>> factory);

    Task<IReadOnlyCollection<XActionOption>> GetOptionsAsync(string sourceKey, CancellationToken cancellationToken);
}

/// <summary>
/// Динамический источник вариантов выбора для аргументов-селекторов.
/// </summary>
public interface IXActionOptionsSource
{
    string Key { get; }
    Task<IReadOnlyCollection<XActionOption>> GetOptionsAsync(CancellationToken cancellationToken);
}

public interface IXActionCommandsProvider
{
    Task<IReadOnlyCollection<XActionCommand>> ReadCommands();
    Task<XActResult> RunCommand(XActionCommand action, IReadOnlyDictionary<string, string> args, CancellationToken cancellationToken);

}
