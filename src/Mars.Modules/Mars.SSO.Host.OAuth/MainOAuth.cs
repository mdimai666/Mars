using Mars.Options.Abstractions.Services;
using Mars.Server.Abstractions.Validators;
using Mars.SSO.Contracts.Options;
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
        services.AddDbContext<SsoAuthDbContext>(opt => opt.UseInMemoryDatabase("oauth"));

        services.AddScoped<IOAuthService, OAuthService>();

        services.AddSingleton<IOAuthClientStore, InMemoryClientStore>();
        services.AddControllersWithViews()
                .AddApplicationPart(typeof(OAuthPageController).Assembly);

        ValidatorFactory.AddValidatorsFromAssembly(services, typeof(AuthorizeRequestValidator).Assembly);

        return services;
    }

    public static IServiceProvider UseMarsOAuthHost(this IServiceProvider serviceProvider)
    {
        var optionService = serviceProvider.GetRequiredService<IOptionService>();

        optionService.RegisterOption<OpenIDServerOption>();
        return serviceProvider;
    }
}
