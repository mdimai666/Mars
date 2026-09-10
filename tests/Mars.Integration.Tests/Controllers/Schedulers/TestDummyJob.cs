using Microsoft.Extensions.Logging;
using Quartz;

namespace Mars.Integration.Tests.Controllers.Schedulers;

public class TestDummyJob : IJob
{
    private readonly ILogger<TestDummyJob> _logger;
    private readonly ITestDummyTriggerService _triggerService;

    public TestDummyJob(ILogger<TestDummyJob> logger, ITestDummyTriggerService triggerService)
    {
        _logger = logger;
        _triggerService = triggerService;
    }

    public Task Execute(IJobExecutionContext context)
    {
        _logger.LogWarning($"{context.JobDetail.Key.Name}");
        _triggerService.Execute();

        return Task.CompletedTask;
    }
}
