using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Mars.Server.Services;

/// <summary>
/// Удаляет дневные файлы логов app_*.log старше периода хранения
/// (Logging:File:RetentionDays в конфигурации, по умолчанию 30 дней).
/// </summary>
public class LogCleanupJob : IJob
{
    public const string ConfigRetentionDays = "Logging:File:RetentionDays";
    public const int DefaultRetentionDays = 30;

    private readonly IHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly ILogger<LogCleanupJob> _logger;

    public LogCleanupJob(IHostEnvironment env, IConfiguration config, ILogger<LogCleanupJob> logger)
    {
        _env = env;
        _config = config;
        _logger = logger;
    }

    public Task Execute(IJobExecutionContext context)
    {
        try
        {
            var retentionDays = int.TryParse(_config[ConfigRetentionDays], out var v) && v >= 1
                ? v
                : DefaultRetentionDays;

            var logsDir = Path.Combine(_env.ContentRootPath, "data", "logs");
            var deleted = LogFileFilter.DeleteFilesOlderThan(logsDir, DateTime.Today.AddDays(-retentionDays));

            if (deleted > 0)
                _logger.LogInformation("logs cleanup: removed {Count} file(s) older than {Days} days", deleted, retentionDays);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "logs cleanup failed");
        }

        return Task.CompletedTask;
    }
}
