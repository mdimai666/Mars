using Mars.Host.Shared.Constants;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Mars.UseStartup.MarsParts;

internal static class MarsStartupPartMetrics
{
    const string SectionName = "OpenTelemetryAppOptions";

    public static IServiceCollection MarsAddMetrics(this IServiceCollection services, IConfiguration configuration)
    {
        var otelOptions = configuration.GetSection(SectionName).Get<OpenTelemetryAppOptions>();

        if (!otelOptions.Enable) return services;

        Action<ResourceBuilder> appResourceBuilder =
            resource => resource
                .AddService(MetricsConstants.AppName)
                .AddContainerDetector()
                .AddHostDetector();

        var otel = services.AddOpenTelemetry();

        otel.WithMetrics(metrics =>
        {
            metrics.AddAspNetCoreInstrumentation();
            metrics.AddMeter(MetricsConstants.AppName);
            metrics.AddMeter("Microsoft.AspNetCore.Hosting");
            metrics.AddMeter("Microsoft.AspNetCore.Server.Kestrel");

            if (otelOptions.AddProcessInstrumentation)
                metrics.AddProcessInstrumentation();
            if (otelOptions.AddRuntimeInstrumentation)
                metrics.AddRuntimeInstrumentation();
            if (otelOptions.AddPrometheusExporter)
                metrics.AddPrometheusExporter();
        });

        otel.ConfigureResource(appResourceBuilder)
            .WithTracing(tracerBuilder =>
            {
                tracerBuilder
                    .AddSource(MetricsConstants.AppName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (otelOptions.AddEntityFrameworkCoreInstrumentation)
                    tracerBuilder.AddEntityFrameworkCoreInstrumentation();
            });

        return services;
    }

    public static WebApplication MarsUseMetrics(this WebApplication app)
    {
        var otelOptions = app.Configuration.GetSection(SectionName).Get<OpenTelemetryAppOptions>();

        if (otelOptions.Enable)
        {
            if (otelOptions.AddPrometheusExporter)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(otelOptions.PrometheusScrapingEndpoint,
                    $"config: {SectionName}.{nameof(OpenTelemetryAppOptions.PrometheusScrapingEndpoint)} must be url");
                app.UseOpenTelemetryPrometheusScrapingEndpoint(otelOptions.PrometheusScrapingEndpoint);
            }
        }

        return app;
    }
}

public class OpenTelemetryAppOptions
{
    public bool Enable { get; set; }
    public bool AddProcessInstrumentation { get; set; } = true;
    public bool AddRuntimeInstrumentation { get; set; }
    public bool AddEntityFrameworkCoreInstrumentation { get; set; }
    public bool AddPrometheusExporter { get; set; } = true;
    public string PrometheusScrapingEndpoint { get; set; } = "/_ot/metrics";
}
