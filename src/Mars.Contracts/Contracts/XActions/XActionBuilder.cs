namespace Mars.Contracts.XActions;

/// <summary>
/// Fluent-сборщик декларативных метаданных XAction с привязкой исполнения
/// (Handler&lt;TAct&gt; или Link). Используется в императивных точках регистрации
/// команд (ConfigureActions веб-приложения, Use*-методы модулей, плагины).
/// </summary>
public sealed class XActionBuilder
{
    string? _id;
    string? _label;
    string? _category;
    string? _description;
    string? _icon;
    bool _system;
    string[]? _frontContexts;
    string _contextMenuGroupId = "";
    float _contextMenuOrder;
    string? _linkValue;
    Type? _handlerType;
    bool _frontAction;
    int? _recommended;
    readonly List<XActionArgument> _arguments = [];

    public XActionBuilder Id(string id)
    {
        _id = id;
        return this;
    }

    public XActionBuilder Label(string label)
    {
        _label = label;
        return this;
    }

    public XActionBuilder Category(string category)
    {
        _category = category;
        return this;
    }

    public XActionBuilder Description(string description)
    {
        _description = description;
        return this;
    }

    public XActionBuilder Icon(string icon)
    {
        _icon = icon;
        return this;
    }

    public XActionBuilder System(bool value = true)
    {
        _system = value;
        return this;
    }

    /// <summary>
    /// Пометить команду как рекомендуемую в палитре (режим «&gt;» без ввода).
    /// Чем больше <paramref name="priority"/>, тем выше команда в топе.
    /// </summary>
    public XActionBuilder Recommended(int priority = 1)
    {
        _recommended = priority;
        return this;
    }

    public XActionBuilder FrontContexts(params string[] contexts)
    {
        _frontContexts = contexts;
        return this;
    }

    public XActionBuilder ContextMenu(string groupId, float order)
    {
        _contextMenuGroupId = groupId;
        _contextMenuOrder = order;
        return this;
    }

    /// <summary>
    /// Объявить аргумент команды.
    /// Статические варианты выбора — в <paramref name="options"/> (отдаются сразу);
    /// динамические — <paramref name="optionsSource"/> (ключ источника, фронт
    /// запросит варианты перед отрисовкой формы).
    /// </summary>
    public XActionBuilder Argument(
        string name,
        string label,
        XActionArgumentType type = XActionArgumentType.String,
        bool required = false,
        string? defaultValue = null,
        string? optionsSource = null,
        params XActionOption[] options)
    {
        _arguments.Add(new XActionArgument
        {
            Name = name,
            Label = label,
            Type = type,
            Required = required,
            DefaultValue = defaultValue,
            OptionsSource = optionsSource,
            Options = options.Length > 0 ? options : null,
        });
        return this;
    }

    /// <summary>
    /// Привязка исполнения: команда выполняется Act-хэндлером,
    /// зарегистрированным в DI (см. AddXActionHandlers).
    /// </summary>
    public XActionBuilder Handler<TAct>() where TAct : class, IAct
    {
        _handlerType = typeof(TAct);
        return this;
    }

    /// <summary>
    /// Команда-ссылка: исполнение — навигация на Url, хэндлер не требуется.
    /// </summary>
    public XActionBuilder Link(string url)
    {
        _linkValue = url;
        return this;
    }

    /// <summary>
    /// Фронтовое действие: исполняется на клиенте через реестр раннеров
    /// (<c>IFrontActionRunner</c>), хэндлер и ссылка не требуются.
    /// </summary>
    public XActionBuilder FrontAction()
    {
        _frontAction = true;
        return this;
    }

    public XActionCommand Build(out Type? handlerType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(_id, "XAction Id");
        ArgumentException.ThrowIfNullOrWhiteSpace(_label, "XAction Label");

        XActionType type;
        if (_frontAction)
        {
            if (_handlerType is not null || !string.IsNullOrEmpty(_linkValue))
                throw new InvalidOperationException($"XAction '{_id}': FrontAction нельзя совмещать с Handler или Link");

            type = XActionType.FrontAction;
        }
        else
        {
            if (_handlerType is null && string.IsNullOrEmpty(_linkValue))
                throw new InvalidOperationException($"XAction '{_id}': не задана привязка исполнения — укажите Handler<TAct>(), Link(url) или FrontAction()");

            if (_handlerType is not null && !string.IsNullOrEmpty(_linkValue))
                throw new InvalidOperationException($"XAction '{_id}': одновременно заданы Handler и Link");

            type = string.IsNullOrEmpty(_linkValue) ? XActionType.HostAction : XActionType.Link;
        }

        handlerType = _frontAction ? null : _handlerType;

        return new XActionCommand
        {
            Id = _id,
            Label = _label,
            Category = _category,
            Description = _description,
            Icon = _icon,
            System = _system,
            Recommended = _recommended,
            FrontContextId = _frontContexts,
            ContextMenuGroupId = _contextMenuGroupId,
            ContextMenuOrder = _contextMenuOrder,
            LinkValue = _linkValue,
            Type = type,
            Arguments = _arguments.Count > 0 ? [.. _arguments] : null,
        };
    }
}
