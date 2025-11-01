using Mars.Host.Shared.Validators;
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

        services.AddSingleton<TokenService>();
        services.AddSingleton<IOAuthClientStore, InMemoryClientStore>();
        services.AddControllersWithViews()
                .AddApplicationPart(typeof(OAuthPageController).Assembly);

        ValidatorFabric.AddValidatorsFromAssembly(services, typeof(AuthorizeRequestValidator).Assembly);

        return services;
    }
}
