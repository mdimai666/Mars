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

    public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        _logger.LogWarning($"{context.JobDetail.Key.Name}");

        return ValueTask.CompletedTask;
    }
}
