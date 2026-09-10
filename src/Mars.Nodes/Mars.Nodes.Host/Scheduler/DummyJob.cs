using Mars.Server.Abstractions.Services;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Mars.Nodes.Host.Scheduler;

internal class DummyJob : IJob
{
    private readonly ILogger<DummyJob> _logger;

    public DummyJob()
    {
        _logger = MarsLogger.GetStaticLogger<DummyJob>();
    }

    public Task Execute(IJobExecutionContext context)
    {
        _logger.LogWarning($"{context.JobDetail.Key.Name}");

        return Task.CompletedTask;
    }
}
