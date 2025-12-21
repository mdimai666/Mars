using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.Shared.Extensions;

public static class FeatureExtensions
{
    public const string SectionName = "FeatureManagement";

    public static bool IsFeatureEnabled(this WebApplicationBuilder builder, string featureName)
    {
        return builder.Configuration.GetSection("FeatureManagement")
            .GetValue<bool>(featureName, false);
    }

    public static bool IsFeatureEnabled(this IApplicationBuilder app, string featureName)
    {
        var config = app.ApplicationServices.GetRequiredService<IConfiguration>();
        return config.GetSection("FeatureManagement").GetValue<bool>(featureName, false);
    }

    public static WebApplicationBuilder AddIfFeatureEnabled(
    this WebApplicationBuilder builder,
    string featureName,
    Action<WebApplicationBuilder> configure)
    {
        if (builder.IsFeatureEnabled(featureName))
            configure(builder);

        return builder;
    }

    public static IApplicationBuilder UseIfFeatureEnabled(
        this IApplicationBuilder app,
        string featureName,
        Action<IApplicationBuilder> configure)
    {
        if (app.IsFeatureEnabled(featureName))
            configure(app);

        return app;
    }

    public static WebApplication UseIfFeatureEnabled(
        this WebApplication app,
        string featureName,
        Action<WebApplication> configure)
    {
        if (app.IsFeatureEnabled(featureName))
            configure(app);

        return app;
    }
}
