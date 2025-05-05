using Mars.Host.Shared.Scheduler;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.AspNetCore;

namespace Mars.Scheduler.Host;

public static class Main
{
    public static IServiceCollection AddMarsScheduler(this IServiceCollection services)
    {
        services.AddQuartz(q =>
        {
            // base Quartz scheduler, job and trigger configuration

        });

        // ASP.NET Core hosting
        services.AddQuartzServer(options =>
        {
            // when shutting down we want jobs to complete gracefully
            options.WaitForJobsToComplete = true;
        });

        services.AddSingleton<ISchedulerManager, SchedulerManager>();

        return services;
    }

    public static IApplicationBuilder UseMarsScheduler(this WebApplication app)
    {

        return app;
    }
}
