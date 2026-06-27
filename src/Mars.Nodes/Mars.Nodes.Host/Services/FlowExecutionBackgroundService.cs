using Mars.Host.Shared.Hubs;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core;
using Mars.Nodes.Host.Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;

namespace Mars.Nodes.Host.Services;

//Not used
internal class FlowExecutionBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<ChatHub> _hub;
    private readonly INodeService _nodeService;
    private readonly INodeRuntime _runtime;

    string nodeId = default!;
    NodeMsg? msg;

    public FlowExecutionBackgroundService(IServiceProvider serviceProvider,
        IHubContext<ChatHub> hub, INodeService nodeService, INodeRuntime runtime)
    {
        _serviceProvider = serviceProvider;
        _hub = hub;
        _nodeService = nodeService;
        _runtime = runtime;
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
        //NodeTaskManager manager = new NodeTaskManager(scope.ServiceProvider, hub, (nodeService as NodeService).Nodes, rns);

        //manager.Run(nodeId, msg);

    }
}
