using System.Reflection;
using Mars.Core.Extensions;
using Mars.Host.Managers;
using Mars.Host.Shared.Managers;
using Mars.Shared.Contracts.XActions;

namespace Mars.XActions;

internal class ActActionsProvider : IXActionCommandsProvider, IActActionsProvider
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ActActionsProvider> _logger;
    ActLocator _actLocator;

    public ActActionsProvider(IServiceScopeFactory serviceScopeFactory, ILogger<ActActionsProvider> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _actLocator = new ActLocator();
    }

    public void RegisterAssembly(Assembly assembly)
    {
        _actLocator.RegisterAssembly(assembly);
    }

    public Task<IReadOnlyCollection<XActionCommand>> ReadCommands()
    {
        return Task.FromResult<IReadOnlyCollection<XActionCommand>>(
            _actLocator.ActItems.Select(x => new XActionCommand() { Id = x.Attr.ActionId, Label = x.Attr.Label, Type = XActionType.HostAction }).ToList()
        );
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
