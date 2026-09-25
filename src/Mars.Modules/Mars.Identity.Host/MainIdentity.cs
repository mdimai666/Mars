using System.Reflection;
using System.Threading.RateLimiting;
using Mars.CommandLine.Abstractions;
using Mars.Data.Entities;
using Mars.Identity.Abstractions.Dto.Users;
using Mars.Identity.Abstractions.Interfaces;
using Mars.Identity.Abstractions.Services;
using Mars.Identity.Contracts.Options;
using Mars.Identity.Host.CommandLine;
using Mars.Identity.Host.Locators;
using Mars.Identity.Host.Models;
using Mars.Identity.Host.Services;
using Mars.Options.Abstractions.Services;
using Mars.Server.Abstractions.Validators;
using Mars.Server.Contracts.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Mars.Identity.Host;

public static class MainIdentity
{
    public static IServiceCollection AddMarsIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());

        ValidatorFactory.AddValidatorsFromAssembly(services, typeof(CreateUserQueryValidator).Assembly);

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.JwtSectionKey));

        services.AddRateLimiter(rateLimitOptions =>
        {
            rateLimitOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            rateLimitOptions.AddPolicy(AuthProtectionOption.RateLimitPolicyName, httpContext =>
            {
                var protection = httpContext.RequestServices.GetRequiredService<IOptionService>()
                    .GetOption<AuthProtectionOption>();

                if (!protection.Enabled)
                {
                    return RateLimitPartition.GetNoLimiter("disabled");
                }

                var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = protection.RateMaxRequestsPerWindow,
                    Window = TimeSpan.FromSeconds(protection.RateWindowSeconds),
                    QueueLimit = 0,
                });
            });
        });

        services.AddOptions<IdentityPasskeyOptions>()
            .Configure<IOptionService>((options, optionService) =>
            {
                var siteUrl = optionService.GetOption<SiteSettings>().SiteUrl;
                if (Uri.TryCreate(siteUrl, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.Host))
                    options.ServerDomain = uri.Host;
            });

        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<IKeyMaterialService, KeyMaterialService>();
        services.AddSingleton<IUserMetaLocator, UserMetaLocator>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IApiKeyService, ApiKeyService>();
        services.AddScoped<IPasskeyService, PasskeyService>();
        services.AddScoped<IUserTypeService, UserTypeService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IAccountsService, AccountsService>();
        services.AddScoped<IExperimentalSignInService, ExperimentalSignInService>();
        services.AddScoped<IRequestContext, RequestContext>();
        services.AddScoped<IUserClaimsPrincipalFactory<UserEntity>, AppClaimsPrincipalFactory>();

        return services;
    }

    public static IApplicationBuilder UseMarsIdentity(this WebApplication app)
    {
        var optionService = app.Services.GetRequiredService<IOptionService>();
        optionService.RegisterOption<PasskeyOption>(appendToInitialSiteData: true);
        optionService.RegisterOption<AuthProtectionOption>(onChangeHook: protection => ApplyLockoutSettings(app.Services, protection));
        ApplyLockoutSettings(app.Services, optionService.GetOption<AuthProtectionOption>());

        var cli = app.Services.GetService<ICommandLineApi>();
        cli?.Register<UserCommandCli>();
        cli?.Register<RoleCommandCli>();

        return app;
    }

    private static void ApplyLockoutSettings(IServiceProvider services, AuthProtectionOption protection)
    {
        var lockout = services.GetRequiredService<IOptions<IdentityOptions>>().Value.Lockout;
        lockout.AllowedForNewUsers = true;
        lockout.MaxFailedAccessAttempts = protection.LockoutMaxFailedAccessAttempts;
        lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(protection.LockoutDefaultLockoutTimeSpanMinutes);
    }
}
