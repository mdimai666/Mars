using Mars.Host.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Mars.Nodes.Host.Scheduler;

internal class InjectNodeSchedulerJob : IJob
{
    private readonly INodeService _nodeService;
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger _logger;

    public const string DataKeyNodeId = "NodeId";

    public InjectNodeSchedulerJob(INodeService nodeService, IServiceProvider serviceProvider)
    {
        this._nodeService = nodeService;
        this.serviceProvider = serviceProvider;
        this._logger = MarsLogger.GetStaticLogger<InjectNodeSchedulerJob>();
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

        await _nodeService.Inject(scope.ServiceProvider, nodeId);

    }
}