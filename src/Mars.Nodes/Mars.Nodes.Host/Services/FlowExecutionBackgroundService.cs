using Mars.Host.Shared.Hubs;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mars.Nodes.Host.Services;

//Not used
internal class FlowExecutionBackgroundService : BackgroundService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IHubContext<ChatHub> hub;
    private readonly INodeService nodeService;
    private readonly RED _RED;

    string nodeId = default!;
    NodeMsg? msg;

    public FlowExecutionBackgroundService(IServiceProvider serviceProvider,
        IHubContext<ChatHub> hub, INodeService nodeService, RED _RED)
    {
        this.serviceProvider = serviceProvider;
        this.hub = hub;
        this.nodeService = nodeService;
        this._RED = _RED;
    }

    public void Setup(string nodeId, NodeMsg? msg = null)
    {
        this.nodeId = nodeId;
        this.msg = msg;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        DoWork(stoppingToken);
        return Task.CompletedTask;
    }

    //https://stackoverflow.com/a/61476388/6723966
    private void DoWork(CancellationToken stoppingToken)
    {
        throw new NotImplementedException();
        //using var scope = serviceProvider.CreateScope();
        //NodeTaskManager manager = new NodeTaskManager(scope.ServiceProvider, hub, (nodeService as NodeService).Nodes, _RED);

        //manager.Run(nodeId, msg);

    }
}
