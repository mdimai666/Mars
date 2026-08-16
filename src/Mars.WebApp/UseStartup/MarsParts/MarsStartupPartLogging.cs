using Mars.Services;
using NReco.Logging.File;

namespace Mars.UseStartup.MarsParts;

internal static class MarsStartupPartLogging
{
    public static WebApplicationBuilder MarsAddLogging(this WebApplicationBuilder builder)
    {
        //https://github.com/nreco/logging
        builder.Services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddConfiguration(builder.Configuration);

            // уровни не хардкодим: правила фильтрации (в т.ч. "Logging:File:LogLevel"
            // по алиасу провайдера [ProviderAlias("File")]) приходят из конфигурации
            // и перечитываются на лету при изменении appsettings без перезапуска
            loggingBuilder.AddFile("data/logs/app_{0:yyyy}-{0:MM}-{0:dd}.log", fileLoggerOpts =>
            {
                fileLoggerOpts.FormatLogFileName = fName =>
                {
                    return String.Format(fName, DateTime.Now);
                };

                // предохранитель: NReco открывает файл жадно и без обработчика бросает исключение —
                // процесс не должен умирать из-за недоступного лога (нет прав, файл залочен).
                // Не подставляем fallback-файл: ошибка глотается, файловый лог этого процесса отключается
                fileLoggerOpts.HandleFileError = fileErr =>
                {
                    Console.WriteLine($"mars: file logging disabled ({fileErr.ErrorException.Message})");
                };
            });
        });

        builder.Services.AddSingleton<LogMaintenanceStartupService>();

        return builder;
    }

    //public static WebApplication MarsUseLogging(this WebApplication app)
    //{

    //    return app;
    //}
}
