using Mars.Core.Models;

namespace Mars.Contracts.XActions;

public record XActionCommand
{
    public required string Id { get; init; }
    public required string Label { get; init; }

    public string? Category { get; init; }
    public string? Description { get; init; }
    public string? Icon { get; init; }

    /// <summary>
    /// Системная команда: скрыта из палитры и контекстных меню,
    /// вызывается только программно (потоки, API, код).
    /// </summary>
    public bool System { get; init; }

    /// <summary>
    /// Рекомендуемая команда в палитре (режим «&gt;» без ввода): чем больше значение,
    /// тем выше в топе. null/0 — не рекомендуемая.
    /// </summary>
    public int? Recommended { get; init; }

    public XActionType Type { get; init; }
    public string? LinkValue { get; init; }

    public string ContextMenuGroupId { get; init; } = "";
    public float ContextMenuOrder { get; init; }
    public string[]? FrontContextId { get; init; }

    public XActionArgument[]? Arguments { get; init; }

}

public record XActionArgument
{
    public required string Name { get; init; }
    public required string Label { get; init; }
    public XActionArgumentType Type { get; init; } = XActionArgumentType.String;
    public bool Required { get; init; }
    public string? DefaultValue { get; init; }

    /// <summary>
    /// Статические варианты выбора — отдаются сразу вместе со схемой.
    /// </summary>
    public XActionOption[]? Options { get; init; }

    /// <summary>
    /// Ключ динамического источника вариантов: фронт запрашивает их
    /// (GET /api/Act/options/{ключ}) перед отрисовкой формы.
    /// </summary>
    public string? OptionsSource { get; init; }
}

/// <summary>
/// Вариант выбора: в вызов передаётся <see cref="Key"/>,
/// <see cref="Label"/> — только отображение (локализация).
/// </summary>
public record XActionOption
{
    public required string Key { get; init; }
    public string? Label { get; init; }

    public static implicit operator XActionOption(string key) => new() { Key = key };
}

public enum XActionArgumentType : int
{
    String = 0,
    Number,
    Bool,
    Choice,
}

public class XActResult : IUserActionResult
{
    public bool Ok { get; init; }
    public required string Message { get; init; }
    public MessageIntent MessageIntent { get; init; }

    /// <summary>
    /// Рекомендованные следующие шаги (навигация, событие, следующее действие).
    /// Интерпретирует вызывающий; автоцепочки не выполняются.
    /// </summary>
    public IReadOnlyList<XActionEffect> Effects { get; init; } = [];

    public static XActResult ToastSuccess(string message)
        => new() { Ok = true, Message = message, MessageIntent = MessageIntent.Success };

    public static XActResult ToastError(string message)
        => new() { Message = message, MessageIntent = MessageIntent.Error };

    public static XActResult ToastWarning(string message)
        => new() { Message = message, MessageIntent = MessageIntent.Warning };

    public static XActResult ToastInfo(string message)
        => new() { Message = message, MessageIntent = MessageIntent.Info };

    public XActResult WithEffect(XActionEffect effect) => new()
    {
        Ok = Ok,
        Message = Message,
        MessageIntent = MessageIntent,
        Effects = [.. Effects, effect],
    };

    public XActResult WithNavigate(string url)
        => WithEffect(new NavigateEffect(url));

    public XActResult WithEvent(string name, System.Text.Json.JsonElement? payload = null)
        => WithEffect(new TriggerEventEffect(name, payload));

    /// <summary>
    /// Рекомендованное следующее действие. Автоматически не выполняется —
    /// вызывающий сам решает, запускать ли его.
    /// </summary>
    public XActResult Then(string actionId, IReadOnlyDictionary<string, string>? args = null)
        => WithEffect(new NextActionEffect(actionId, args));
}

public record XActionCommandCall
{
    public required string Id { get; init; }
    public IReadOnlyDictionary<string, string> Args { get; init; } = new Dictionary<string, string>();
}

public interface IActContext
{
    IReadOnlyDictionary<string, string> Args { get; }

    string? Get(string name)
        => Args.TryGetValue(name, out var value) ? value : null;
}

public record ActContext(IReadOnlyDictionary<string, string> Args) : IActContext;

public interface IAct
{
    public Task<XActResult> Execute(IActContext context, CancellationToken cancellationToken);
}

public enum XActionType : int
{
    Link = 0,
    HostAction = 1,

    /// <summary>
    /// Фронтовое действие: метаданные зарегистрированы как обычно (система видит команду),
    /// но исполнение происходит на клиенте через реестр раннеров — хост такую команду не выполняет.
    /// </summary>
    FrontAction = 2,
}
