using System.Net;
using Flurl.Http;
using Mars.Contracts.Common;
using Mars.Data.Infrastructure;
using Mars.Identity.Abstractions.Services;
using Mars.Identity.Host.Models;
using Mars.Nodes.Abstractions.Hubs;
using Mars.Options.Abstractions.Services;
using Mars.Server.Abstractions.Extensions;
using Mars.Server.Abstractions.Features;
using Mars.Server.Contracts.Options;
using Mars.SSO.Host.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Mars.UseStartup.MarsParts;

internal static class MarsStartupPartCore
{
    public static IServiceCollection MarsAddCore(this IServiceCollection services, ConfigurationManager configuration)
    {
        services.AddHttpClient();
        services.AddHttpClient<IFlurlClient, FlurlClient>();

        //------------------------------------------
        // Core

        services.AddCors(options => //not check
        {
            options.AddDefaultPolicy(
                builder => builder
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
            );
        });

        //TODO: think
        //AppSharedSettings.BackendUrl = "";
        //AppSharedSettings.Program = typeof(Mars.Admin.Program);
        var conn = configuration.GetConnectionString("DefaultConnection");

        services.AddMarsDataInfrastructure(configuration);

        // https://source.dot.net/#Microsoft.AspNetCore.Identity.EntityFrameworkCore/IdentityEntityFrameworkBuilderExtensions.cs,90
        // services.TryAddScoped(typeof(IUserStore<>).MakeGenericType(userType), userStoreType);

        var jwtSettings = configuration.GetSection(JwtSettings.JwtSectionKey).Get<JwtSettings>();

        services
            .AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = "smart";
                opt.DefaultChallengeScheme = "smart";
            })
            .AddPolicyScheme("smart", "Authorization Bearer or OIDC", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                    if (authHeader?.ToLower().StartsWith("bearer ") == true)
                    {
                        return JwtBearerDefaults.AuthenticationScheme;
                    }
                    return IdentityConstants.ApplicationScheme;
                };
            })
            .AddCookie()
            .AddJwtBearer();

        bool isSsoEnabled = configuration.GetSection(FeatureExtensions.SectionName).GetValue<bool>(FeatureFlags.SingleSignOn, false);

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IKeyMaterialService, IOptions<JwtSettings>, IOptionService, IServiceProvider>((options, keys, jwtSettings, ops, sp) =>
            {
                var sso = isSsoEnabled ? sp.GetRequiredService<ISsoProviderRepository>() : null;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    //ValidIssuer = jwtSettings.Value.ValidIssuer,
                    ValidAudience = jwtSettings.Value.ValidAudience,
                    IssuerSigningKey = keys.SigningKey,

                    IssuerValidator = (issuer, token, parameters) =>
                    {
                        if (issuer == ops.GetOption<SiteSettings>().SiteUrl)
                            return issuer;
                        else if (isSsoEnabled && sso.TryValidateIssuer(issuer, out var validIssuer))
                            return issuer;
                        throw new SecurityTokenInvalidIssuerException($"Invalid issuer: {issuer}");
                    },
                };
            });

        services.ConfigureApplicationCookie(options =>
        {
            //options.Cookie.SameSite = SameSiteMode.Unspecified;
            //options.Cookie.HttpOnly = false;
            int expInMinutes = jwtSettings.ExpiryInMinutes;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(expInMinutes);
            options.SlidingExpiration = true;

            options.Events = new CookieAuthenticationEvents()
            {
                OnRedirectToLogin = async (ctx) =>
                {
                    if (ctx.Request.Path.StartsWithSegments("/api") && ctx.Response.StatusCode == 200)
                    {
                        ctx.Response.StatusCode = 401;
                    }

                    await ctx.Response.WriteAsJsonAsync(new UserActionResult
                    {
                        Ok = false,
                        Message = HttpStatusCode.Unauthorized.ToString()
                    });
                },
                OnRedirectToAccessDenied = async (ctx) =>
                {
                    if (ctx.Request.Path.StartsWithSegments("/api") && ctx.Response.StatusCode == 200)
                    {
                        ctx.Response.StatusCode = 403;
                    }

                    await ctx.Response.WriteAsJsonAsync(new UserActionResult
                    {
                        Ok = false,
                        Message = HttpStatusCode.Unauthorized.ToString()
                    });
                }
            };
        });

        return services;
    }

    public static IServiceCollection AddMarsSignalRConfiguration(this IServiceCollection services)
    {
        services
            .AddSignalR(hubOptions =>
            {
                hubOptions.EnableDetailedErrors = true;
                // Должен быть меньше server timeout клиентов (по умолчанию 30 с и у JS, и у .NET
                // клиента): иначе в паузах без сообщений (долгие запуски ИИ-агента, ожидание
                // инструментов) соединения рвутся с "Server timeout elapsed without receiving
                // a message from the server".
                hubOptions.KeepAliveInterval = TimeSpan.FromSeconds(15);
            })
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNamingPolicy = null;
            });
        services.AddSingleton<BroadcastHub>();

        return services;
    }

}
