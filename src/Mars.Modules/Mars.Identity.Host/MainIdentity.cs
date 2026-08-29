using System.Reflection;
using Mars.CommandLine.Abstractions;
using Mars.Data.Entities;
using Mars.Identity.Abstractions.Dto.Users;
using Mars.Identity.Abstractions.Interfaces;
using Mars.Identity.Abstractions.Services;
using Mars.Identity.Host.CommandLine;
using Mars.Identity.Host.Locators;
using Mars.Identity.Host.Models;
using Mars.Identity.Host.Services;
using Mars.Server.Abstractions.Validators;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Identity.Host;

public static class MainIdentity
{
    public static IServiceCollection AddMarsIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());

        ValidatorFactory.AddValidatorsFromAssembly(services, typeof(CreateUserQueryValidator).Assembly);

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

    public static IApplicationBuilder UseMarsIdentity(this WebApplication app)
    {
        var cli = app.Services.GetService<ICommandLineApi>();
        cli?.Register<UserCommandCli>();
        cli?.Register<RoleCommandCli>();

        return app;
    }
}
