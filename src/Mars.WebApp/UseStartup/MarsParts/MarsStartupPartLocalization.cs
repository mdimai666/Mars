using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

namespace Mars.UseStartup.MarsParts;

internal static class MarsStartupPartLocalization
{
    public static IServiceCollection MarsAddLocalization(this IServiceCollection services)
    {
        var defaultCulture = new CultureInfo("ru");

        services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCultures = new[]
             {
                new CultureInfo("ru"),
                //new CultureInfo("en"),
            };
            options.DefaultRequestCulture = new RequestCulture("ru-RU");
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;

            options.ApplyCurrentCultureToResponseHeaders = true;
        });

        var cultureInfo = defaultCulture;
        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
        Thread.CurrentThread.CurrentCulture = cultureInfo;
        Thread.CurrentThread.CurrentUICulture = cultureInfo;

        return services;
    }

    public static WebApplication MarsUseLocalization(this WebApplication app)
    {

        var localizationOptions = app.Services.GetService<IOptions<RequestLocalizationOptions>>().Value;
        app.UseRequestLocalization(localizationOptions);

        return app;
    }


}
