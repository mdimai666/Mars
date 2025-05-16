using Mars.Host.Shared.Scheduler;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Startup;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mars.Nodes.Host.Scheduler;


internal class NodeSchedulerService : INodeSchedulerService, IMarsAppLifetimeService
{
    protected readonly IServiceProvider _serviceProvider;
    protected readonly INodeService _nodeService;
    readonly ILogger _logger;

    public NodeSchedulerService(IServiceProvider serviceProvider, INodeService nodeService)
    {
        _serviceProvider = serviceProvider;
        _logger = MarsLogger.GetStaticLogger<NodeSchedulerService>();
        _nodeService = nodeService;

        _nodeService.OnDeploy += _nodeService_OnDeploy;
    }

    private void _nodeService_OnDeploy()
    {
        _ = UpdateScheduledInjectNodes();
    }

    //public List<InjectNodeImpl> GetScheduledNodes() => _nodeService.Nodes.Where(node => node is InjectNodeImpl injectNodeImpl && !injectNodeImpl.Node.Disabled && injectNodeImpl.Node.IsSchedule).Select(node => (InjectNodeImpl)node).ToList();
    public List<InjectNode> GetScheduledNodes() => _nodeService.BaseNodes.Where(node => node is InjectNode injectNode && !injectNode.Disabled && injectNode.IsSchedule).Select(node => (InjectNode)node).ToList();

    [StartupOrder(11)]
    public Task OnStartupAsync()
    {
        _ = StartScheduledInjectNodes();

        return Task.CompletedTask;
    }

    string GetNodeAsJobName(Node node) => $"node:{node.Label}-{node.Id}";

    async Task StartScheduledInjectNodes()
    {
        try
        {
            var scheduler = _serviceProvider.GetRequiredService<ISchedulerManager>();

#if DEBUG
            //await scheduler.AddIntervalJob<DummyJob>("test", "nodes", TimeSpan.FromSeconds(6));
            //await scheduler.AddJob<DummyJob>("test2", "nodes", "0 0/10 * * * ?");
#endif

            var scheduledNodes = GetScheduledNodes();

            if (scheduledNodes.Count == 0) return;

            _logger.LogInformation($"scheduledNodes {scheduledNodes.Count} nodes");

            var tasks = scheduledNodes.Select(async node =>
            {
                var data = new Dictionary<string, object>() { [InjectNodeSchedulerJob.DataKeyNodeId] = node.Id };
                await scheduler.AddJob<InjectNodeSchedulerJob>(GetNodeAsJobName(node), "nodes", node.ScheduleCronMask, data);
            }).ToArray();

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }

    async Task UpdateScheduledInjectNodes()
    {
        _logger.LogInformation("UpdateScheduledInjectNodes");
        var scheduler = _serviceProvider.GetRequiredService<ISchedulerManager>();
        await scheduler.DeleteJobGroup("nodes");
        await StartScheduledInjectNodes();

    }
}
