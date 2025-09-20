using Mars.Core.Exceptions;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Startup;
using Mars.Shared.Contracts.XActions;
using Microsoft.Extensions.Logging;

namespace Mars.Host.Managers;

/// <summary>
/// Singletone service
/// </summary>
internal class XActionManager : IActionManager, IMarsAppLifetimeService
{
    Dictionary<string, XActionCommand> _registeredActions = [];
    Dictionary<string, XActionCommandContext> _allActions = [];
    private readonly ILogger<XActionManager> _logger;

    //HashSet<Assembly> assemblies = [];
    Lock _lockRefreshDict = new();
    bool invalide = true;

    List<IXActionCommandsProvider> _xActionCommandsProviders = [];

    public XActionManager(ILogger<XActionManager> logger)
    {
        _logger = logger;
    }

    public void AddXLink(XActionCommand xAction)
    {
        if (xAction.Type != XActionType.Link) throw new ArgumentException("not valid type");
        if (string.IsNullOrEmpty(xAction.LinkValue)) throw new ArgumentNullException("LinkValue cannot be empty for link");
        _registeredActions.Add(xAction.Id, xAction);
        invalide = true;
    }

    public void AddAction(XActionCommand xAction)
    {
        _registeredActions.Add(xAction.Id, xAction);
        invalide = true;
    }

    public void AddActionsProvider(IXActionCommandsProvider actionCommandsProvider)
    {
        _xActionCommandsProviders.Add(actionCommandsProvider);
        invalide = true;
    }

    public IReadOnlyDictionary<string, XActionCommand> XActions
    {
        get
        {
            if (invalide) RefreshDict().Wait();
            return _allActions.Values.ToDictionary(s => s.Command.Id, s => s.Command);
        }
    }

    public async Task<XActResult> Inject(string id, string[] args, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        _logger.LogTrace($"Inject: '{id}'");

        if (!_allActions.TryGetValue(id, out var action)) throw new NotFoundException("ActionManager: action not found");

        ArgumentNullException.ThrowIfNull(action.Provider);

        return await action.Provider.RunCommand(action.Command, args);
    }

    public async Task RefreshDict(bool force = false)
    {
        if (!invalide && !force) return;

        List<XActionCommandContext> providersCommands = [];

        foreach (var provider in _xActionCommandsProviders)
            foreach (var action in await provider.ReadCommands())
                providersCommands.Add(new XActionCommandContext { Command = action, Provider = provider });

        if (!invalide && !force) return;

        using (_lockRefreshDict.EnterScope())
        {
            _allActions.Clear();

            foreach (var action in _registeredActions.Values)
            {
                _allActions.Add(action.Id, new() { Command = action });
            }

            foreach (var action in providersCommands)
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
}
