using Mars.Host.Shared.Services;
using Mars.Host.Shared.Validators;
using Mars.Options.Models;
using Mars.SSO.Host.OAuth.Controllers;
using Mars.SSO.Host.OAuth.Data;
using Mars.SSO.Host.OAuth.interfaces;
using Mars.SSO.Host.OAuth.Services;
using Mars.SSO.Host.OAuth.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.SSO.Host.OAuth;

public static class MainOAuth
{
    public static IServiceCollection AddMarsOAuthHost(this IServiceCollection services)
    {
        services.AddDbContext<AuthDbContext>(opt => opt.UseInMemoryDatabase("oauth"));

        services.AddScoped<IOAuthService, OAuthService>();

        services.AddSingleton<IOAuthClientStore, InMemoryClientStore>();
        services.AddControllersWithViews()
                .AddApplicationPart(typeof(OAuthPageController).Assembly);

        ValidatorFabric.AddValidatorsFromAssembly(services, typeof(AuthorizeRequestValidator).Assembly);

        return services;
    }

    public static IServiceProvider UseMarsOAuthHost(this IServiceProvider serviceProvider)
    {
        var optionService = serviceProvider.GetRequiredService<IOptionService>();

        optionService.RegisterOption<OpenIDServerOption>();
        return serviceProvider;
    }
}
