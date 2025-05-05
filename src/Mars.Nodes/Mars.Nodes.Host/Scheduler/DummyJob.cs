using Mars.Host.Shared.Services;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Mars.Nodes.Host.Scheduler;

internal class DummyJob : IJob
{
    private readonly ILogger _logger;

    public DummyJob()
    {
        this._logger = MarsLogger.GetStaticLogger<InjectNodeSchedulerJob>();
    }

    public Task Execute(IJobExecutionContext context)
    {
        _logger.LogWarning($"{context.JobDetail.Key.Name}");

        return Task.CompletedTask;
    }
}