using System.Reflection;
using Mars.Scheduler.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Mars.Scheduler.Host;

public static class MainScheduler
{
    public static IServiceCollection AddMarsScheduler(this IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());

        services.AddQuartz(q =>
        {
            // base Quartz scheduler, job and trigger configuration

        });

        services.AddQuartzHostedService(options =>
        {
            // when shutting down we want jobs to complete gracefully
            options.WaitForJobsToComplete = true;
        });

        services.AddSingleton<ISchedulerManager, SchedulerManager>();

        return services;
    }
}
