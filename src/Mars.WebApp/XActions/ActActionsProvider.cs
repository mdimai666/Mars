using System.Reflection;
using Mars.Core.Extensions;
using Mars.Host.Managers;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Startup;
using Mars.Shared.Contracts.XActions;

namespace Mars.XActions;

internal class ActActionsProvider : IXActionCommandsProvider, IActActionsProvider, IMarsAppLifetimeService
{
    private readonly IActionManager _actionManager;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ActActionsProvider> _logger;
    ActLocator _actLocator;

    public ActActionsProvider(IActionManager actionManager, IServiceScopeFactory serviceScopeFactory, ILogger<ActActionsProvider> logger)
    {
        _actionManager = actionManager;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _actLocator = new ActLocator();
        _actLocator.RegisterAssembly(typeof(ClearCacheAct).Assembly);
        _actionManager.AddActionsProvider(this);
    }

    [StartupOrder]
    public Task OnStartupAsync()
    {
        _actLocator.RefreshDict();
        return Task.CompletedTask;
    }

    public void RegisterAssembly(Assembly assembly)
    {
        _actLocator.RegisterAssembly(assembly);
    }

    public Task<IReadOnlyCollection<XActionCommand>> ReadCommands()
    {
        var list = new List<XActionCommand>
        {
            #if DEBUG
		    DummyAct.XAction,
	        #endif
            ClearCacheAct.XAction,
            CreateMockPostsAct.XAction,
        };

        foreach (var act in _actLocator.ActItems)
        {
            var a = act.Attr;

            list.Add(new()
            {
                Id = a.ActionId,
                Label = a.Label,
                Type = XActionType.HostAction,
            });
        }
        return Task.FromResult<IReadOnlyCollection<XActionCommand>>(list);
    }

    public async Task<XActResult> RunCommand(XActionCommand action, string[] args)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var registeredAct = _actLocator.TryGetActionById(action.Id);
        var act = (IAct)ActivatorUtilities.CreateInstance(scope.ServiceProvider, registeredAct.ActType);

        try
        {
            var context = new ActContext { args = args };
            _logger.LogInformation($"Inject: '{act.GetType().FullName}'. args='{args.JoinStr(",")}'");
            return await act.Execute(context, default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return XActResult.ToastError("ActionManager: " + ex.Message);
        }
    }
}
