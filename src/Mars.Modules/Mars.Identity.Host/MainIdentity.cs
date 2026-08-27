using Mars.Host.Data.Entities;
using Mars.Host.Models;
using Mars.Host.Services;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Identity.Host;

public static class MainIdentity
{
    public static IServiceCollection AddMarsIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.JwtSectionKey));

        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<IKeyMaterialService, KeyMaterialService>();
        services.AddSingleton<IUserMetaLocator, UserMetaLocator>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserTypeService, UserTypeService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<AccountsService>();
        services.AddScoped<IAccountsService, AccountsService>();
        services.AddScoped<IExperimentalSignInService, ExperimentalSignInService>();
        services.AddScoped<IRequestContext, RequestContext>();
        services.AddScoped<IUserClaimsPrincipalFactory<UserEntity>, AppClaimsPrincipalFactory>();

        return services;
    }
}
