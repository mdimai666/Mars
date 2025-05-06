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
            loggingBuilder.SetMinimumLevel(LogLevel.Warning);

            loggingBuilder.AddFile("data/logs/app_{0:yyyy}-{0:MM}-{0:dd}.log", fileLoggerOpts =>
            {
                fileLoggerOpts.FormatLogFileName = fName =>
                {
                    return String.Format(fName, DateTime.Now);
                };
                fileLoggerOpts.FilterLogEntry = (msg) =>
                {
                    return msg.LogLevel >= LogLevel.Warning;
                };
            });
        });

        return builder;
    }

    //public static WebApplication MarsUseLogging(this WebApplication app)
    //{

    //    return app;
    //}
}
