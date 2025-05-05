using Mars.Host.Shared.Services;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Mars.Integration.Tests.Controllers.Schedulers;

public class TestDummyJob : IJob
{
    private readonly ILogger _logger;
    private readonly ITestDummyTriggerService _triggerService;

    public TestDummyJob(ITestDummyTriggerService triggerService)
    {
        _logger = MarsLogger.GetStaticLogger<TestDummyJob>();
        _triggerService = triggerService;
    }

    public Task Execute(IJobExecutionContext context)
    {
        _logger.LogWarning($"{context.JobDetail.Key.Name}");
        _triggerService.Execute();

        return Task.CompletedTask;
    }
}
