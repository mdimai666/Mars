using Mars.Scheduler.Abstractions;
using Mars.Server.Abstractions.Startup;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Server.Services;

/// <summary>
/// Регистрирует системную Quartz-джобу обслуживания логов:
/// ежедневная чистка файлов старше периода хранения.
/// </summary>
public class LogMaintenanceStartupService(IServiceProvider serviceProvider) : IMarsAppLifetimeService
{
    [StartupOrder(12)]
    public async Task OnStartupAsync()
    {
        var scheduler = serviceProvider.GetRequiredService<ISchedulerManager>();

        await scheduler.AddDailyJob<LogCleanupJob>("logs-cleanup", "system", new TimeOnly(3, 0));
    }
}
