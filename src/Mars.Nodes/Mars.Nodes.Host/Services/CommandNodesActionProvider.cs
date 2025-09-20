using Mars.Core.Exceptions;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core.Nodes;
using Mars.Shared.Contracts.XActions;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Host.Services;

internal class CommandNodesActionProvider : IXActionCommandsProvider, IDisposable
{
    private readonly INodeService _nodeService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IActionManager _actionManager;

    public CommandNodesActionProvider(INodeService nodeService, IServiceScopeFactory serviceScopeFactory, IActionManager actionManager)
    {
        _nodeService = nodeService;
        _serviceScopeFactory = serviceScopeFactory;
        _actionManager = actionManager;
        _nodeService.OnDeploy += OnNodesDeploy;
    }

    public void Dispose()
    {
        _nodeService.OnDeploy -= OnNodesDeploy;
    }

    void OnNodesDeploy()
    {
        _actionManager.RefreshDict(true);
    }

    public Task<IReadOnlyCollection<XActionCommand>> ReadCommands()
    {
        var nodes = _nodeService.BaseNodes.Values.Where(node => node is ActionCommandNode && !node.Disabled)
                                                    .Select(s=>s as ActionCommandNode)
                                                    .ToList();

        return Task.FromResult<IReadOnlyCollection<XActionCommand>>(nodes.Select(node => new XActionCommand
        {
            Id = node.Id,
            Label = node.Label,
            FrontContextId = node.FrontContextId,
            Type = XActionType.HostAction,
        }).ToList());
    }

    public Task<XActResult> RunCommand(XActionCommand command, string[] args)
    {
        if (_nodeService.BaseNodes.TryGetValue(command.Id, out var node))
        {
            _nodeService.Inject(_serviceScopeFactory, node.Id, new Core.NodeMsg { Payload = args });

            return Task.FromResult(XActResult.ToastSuccess($"command inject '{command.Label}'"));
        }
        else throw new NotFoundException($"node '{command.Id}' not found");
    }
}
