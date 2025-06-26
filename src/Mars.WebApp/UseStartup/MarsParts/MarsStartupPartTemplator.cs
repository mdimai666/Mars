using Mars.Host.Shared.Services;
using Mars.Host.Templators;

namespace Mars.UseStartup.MarsParts;

internal static class MarsStartupPartTemplator
{
    public static IServiceCollection MarsAddTemplator(this IServiceCollection services)
    {
        services.AddSingleton<ITemplatorFeaturesLocator, TemplatorFeaturesLocator>();
        return services;
    }

    public static WebApplication MarsUseTemplator(this WebApplication app)
    {

        ITemplatorFeaturesLocator tflocator = app.Services.GetRequiredService<ITemplatorFeaturesLocator>();

        var functions = tflocator.Functions;

        functions.Add(nameof(TemplatorRegisterFunctions.Paginator), TemplatorRegisterFunctions.Paginator);
        functions.Add(nameof(TemplatorRegisterFunctions.Req), TemplatorRegisterFunctions.Req);
        functions.Add(nameof(TemplatorRegisterFunctions.CalendarRow), TemplatorRegisterFunctions.CalendarRow);

        return app;
    }
}
