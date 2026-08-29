using System.Text.RegularExpressions;
using Mars.Core.Exceptions;
using Mars.Server.Abstractions.Managers;
using Mars.Server.Abstractions.Startup;
using Mars.Contracts.XActions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mars.Server.Managers;

/// <summary>
/// Singletone service
/// </summary>
internal partial class XActionManager : IActionManager, IMarsAppLifetimeService
{
    record RegisteredAction(XActionCommand Command, Type? HandlerType);

    Dictionary<string, RegisteredAction> _registeredActions = [];
    Dictionary<string, XActionCommandContext> _allActions = [];
    private readonly ILogger<XActionManager> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    //HashSet<Assembly> assemblies = [];
    Lock _lockRefreshDict = new();
    bool invalide = true;

    List<IXActionCommandsProvider> _xActionCommandsProviders = [];

    Dictionary<string, IXActionOptionsSource> _optionsSources = [];

    [GeneratedRegex(@"^[A-Za-z][A-Za-z0-9_+\-]*(\.[A-Za-z][A-Za-z0-9_+\-]*)*$")]
    private static partial Regex IdFormatRegex();

    public XActionManager(ILogger<XActionManager> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public void Add(Action<XActionBuilder> configure)
    {
        var builder = new XActionBuilder();
        configure(builder);
        var command = builder.Build(out var handlerType);

        if (!IdFormatRegex().IsMatch(command.Id))
            throw new ArgumentException($"XAction id '{command.Id}' имеет неверный формат (ожидается Owner.Module.Name)");

        _registeredActions.Add(command.Id, new RegisteredAction(command, handlerType));
        invalide = true;
    }

    public void AddFrontContexts(string id, params string[] frontContexts)
    {
        if (!_registeredActions.TryGetValue(id, out var registered))
        {
            _logger.LogWarning($"AddFrontContexts: команда '{id}' не зарегистрирована");
            return;
        }

        var merged = (registered.Command.FrontContextId ?? [])
            .Concat(frontContexts)
            .Distinct()
            .ToArray();

        _registeredActions[id] = registered with { Command = registered.Command with { FrontContextId = merged } };
        invalide = true;
    }

    public void AddActionsProvider(IXActionCommandsProvider actionCommandsProvider)
    {
        _xActionCommandsProviders.Add(actionCommandsProvider);
        invalide = true;
    }

    public void AddOptionsSource(string key, Func<CancellationToken, Task<IReadOnlyCollection<XActionOption>>> factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _optionsSources[key] = new DelegateXActionOptionsSource(key, factory);
    }

    public Task<IReadOnlyCollection<XActionOption>> GetOptionsAsync(string sourceKey, CancellationToken cancellationToken)
    {
        if (!_optionsSources.TryGetValue(sourceKey, out var source))
            throw new NotFoundException($"options source '{sourceKey}' not found");

        return source.GetOptionsAsync(cancellationToken);
    }

    public IReadOnlyDictionary<string, XActionCommand> XActions
    {
        get
        {
            if (invalide) RefreshDict().Wait();
            return _allActions.Values.ToDictionary(s => s.Command.Id, s => s.Command);
        }
    }

    public async Task<XActResult> Inject(string id, IReadOnlyDictionary<string, string> args, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        _logger.LogTrace($"Inject: '{id}'");

        await RefreshDict();

        if (!_allActions.TryGetValue(id, out var action))
            throw new NotFoundException($"command not found: '{id}'");

        if (action.Provider != null)
            return await action.Provider.RunCommand(action.Command, args, cancellationToken);

        if (action.Command.Type == XActionType.FrontAction)
            return XActResult.ToastError($"команда '{id}' — фронтовая: выполняется на клиенте, хост её не исполняет");

        if (action.Command.Type == XActionType.Link)
            return XActResult.ToastWarning($"команда '{id}' — ссылка: {action.Command.LinkValue}");

        if (action.HandlerType is null)
            throw new NotFoundException($"command '{id}' has no handler");

        var error = PrepareArgs(action.Command, args, out var effectiveArgs);
        if (error != null) return error;

        using var scope = _serviceScopeFactory.CreateScope();
        var act = (IAct)(scope.ServiceProvider.GetService(action.HandlerType)
            ?? ActivatorUtilities.CreateInstance(scope.ServiceProvider, action.HandlerType));

        try
        {
            _logger.LogInformation($"Inject: '{act.GetType().FullName}'. args='{string.Join(",", effectiveArgs.Select(kv => $"{kv.Key}={kv.Value}"))}'");
            return await act.Execute(new ActContext(effectiveArgs), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return XActResult.ToastError("ActionManager: " + ex.Message);
        }
    }

    /// <summary>
    /// Дозаполняет аргументы значениями по умолчанию и проверяет обязательные.
    /// </summary>
    XActResult? PrepareArgs(XActionCommand command, IReadOnlyDictionary<string, string> args, out Dictionary<string, string> effectiveArgs)
    {
        effectiveArgs = new Dictionary<string, string>(args);

        foreach (var argument in command.Arguments ?? [])
        {
            if (effectiveArgs.ContainsKey(argument.Name)) continue;

            if (argument.DefaultValue != null)
            {
                effectiveArgs[argument.Name] = argument.DefaultValue;
                continue;
            }

            if (argument.Required)
                return XActResult.ToastError($"для команды '{command.Id}' не передан обязательный аргумент '{argument.Name}'");
        }

        return null;
    }

    public async Task RefreshDict(bool force = false)
    {
        if (!invalide && !force) return;

        Dictionary<string, XActionCommandContext> providersCommands = [];

        foreach (var provider in _xActionCommandsProviders)
            foreach (var action in await provider.ReadCommands())
            {
                if (!providersCommands.TryAdd(action.Id, new XActionCommandContext { Command = action, Provider = provider }))
                {
                    _logger.LogWarning($"providersCommands '{provider.GetType().Name}' try add duplicate command '{action.Id}'");
                }

            }

        if (!invalide && !force) return;

        using (_lockRefreshDict.EnterScope())
        {
            _allActions.Clear();

            foreach (var registered in _registeredActions.Values)
            {
                IXActionCommandsProvider? provider = null;
                if (providersCommands.TryGetValue(registered.Command.Id, out var providedHostCommand))
                {
                    provider = providedHostCommand.Provider;
                }
                _allActions.Add(registered.Command.Id, new()
                {
                    Command = registered.Command,
                    Provider = provider,
                    HandlerType = registered.HandlerType,
                });
            }

            foreach (var action in providersCommands.Values)
            {
                if (_allActions.ContainsKey(action.Command.Id)) continue;
                _allActions.Add(action.Command.Id, action);
            }
        }

        invalide = false;
    }

    [StartupOrder(20)]
    public Task OnStartupAsync()
    {
        _ = RefreshDict();
        return Task.CompletedTask;
    }
}

internal record XActionCommandContext
{
    public required XActionCommand Command { get; init; }
    public IXActionCommandsProvider? Provider { get; init; }
    public Type? HandlerType { get; init; }
}

internal record DelegateXActionOptionsSource(
    string Key,
    Func<CancellationToken, Task<IReadOnlyCollection<XActionOption>>> Factory) : IXActionOptionsSource
{
    public Task<IReadOnlyCollection<XActionOption>> GetOptionsAsync(CancellationToken cancellationToken)
        => Factory(cancellationToken);
}
