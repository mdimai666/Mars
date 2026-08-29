//using Mars.Areas.Identity;
using Mars.Admin.Framework;
using Mars.Server;
using Mars.Server.Abstractions.Services;
using Mars.Services;

namespace Mars.UseStartup.MarsParts;

internal static class MarsStartupPartServices
{
    public static IServiceCollection AddMarsHostServices(this IServiceCollection services, IWebHostEnvironment wenv)
    {
        //services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<UserEntity>>();

        //services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.JwtSectionKey));

        services.AddDatabaseDeveloperPageExceptionFilter();//from clear template

        // basic services
        services.AddSingleton<IDevAdminConnectionService, DevAdminConnectionService>();

        services.AddMarsHost(wenv);

        services.AddAdminFrameworkServerServices();

        return services;
    }

    public static IServiceProvider UseMarsHostServices(this IServiceProvider services)
    {
        services.UseMarsServerOptions();

        return services;
    }

}
