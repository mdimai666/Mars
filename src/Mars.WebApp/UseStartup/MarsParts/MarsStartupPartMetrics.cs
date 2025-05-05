using System.Diagnostics.Metrics;

namespace Mars.UseStartup.MarsParts;

internal static class MarsStartupPartMetrics
{
    static Meter _meter = new Meter("Mars.Services");
    static Counter<int> _controllerTest1 = _meter.CreateCounter<int>("controller.test1");


    public static IServiceCollection MarsAddMetrics(this IServiceCollection services)
    {

        return services;
    }

    public static WebApplication MarsUseMetrics(this WebApplication app)
    {
        // https://learn.microsoft.com/en-us/dotnet/core/diagnostics/metrics-collection
        // dotnet add package OpenTelemetry.Exporter.Prometheus.HttpListener --prerelease

        _controllerTest1.Add(1);


        return app;
    }
}
