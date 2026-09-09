using Mars.Nodes.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Mars.Nodes.Host.Scheduler;

internal class InjectNodeSchedulerJob : IJob
{
    private readonly INodeService _nodeService;
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<InjectNodeSchedulerJob> _logger;

    public const string DataKeyNodeId = "NodeId";

    public InjectNodeSchedulerJob(INodeService nodeService, IServiceProvider serviceProvider, ILogger<InjectNodeSchedulerJob> logger)
    {
        _nodeService = nodeService;
        this.serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var nodeId = context.JobDetail.JobDataMap.GetString(DataKeyNodeId)
                            ?? throw new ArgumentNullException($"key 'DataKeyNodeId' not found");

        //var node = ((NodeService)_nodeService).Nodes.FirstOrDefault(s => s.Node.Id == nodeId)
        //    ?? throw new ArgumentNullException($"node id '{nodeId}' not found");

        //_logger.LogInformation($"{GetNodeAsJobName(node.Node)} - execute");
        _logger.LogInformation($"{context.JobDetail.Key} - execute");

        var scope = serviceProvider.CreateScope(); //TODO: wait complete

        await _nodeService.InjectAsync(scope.ServiceProvider, nodeId);

    }
}
