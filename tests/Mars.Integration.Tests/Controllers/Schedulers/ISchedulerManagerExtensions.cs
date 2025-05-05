using Mars.Host.Shared.Scheduler;

namespace Mars.Integration.Tests.Controllers.Schedulers;

public static class ISchedulerManagerExtensions
{
    public static Task AddTestJob(this ISchedulerManager scheduler, string jobName = "dummyJob")
    {
        return scheduler.AddDailyJob<TestDummyJob>(jobName, "tests", new TimeOnly());
    }
}
