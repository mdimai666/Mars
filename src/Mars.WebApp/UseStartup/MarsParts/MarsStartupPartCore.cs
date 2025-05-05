using System.Net;
using System.Text;
using Flurl.Http;
using Mars.Host.Data.Entities;
using Mars.Host.Infrastructure;
using Mars.Host.Models;
using Mars.Host.Services;
using Mars.Host.Shared.Services;
using Mars.Shared.Common;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Mars.UseStartup.MarsParts;

internal static class MarsStartupPartCore
{
    public static IServiceCollection MarsAddCore(this IServiceCollection services, ConfigurationManager configuration)
    {
        //ConfigurationManager configuration = sonfiguration;
        IOptionService.Configuration = configuration;

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
        //AppSharedSettings.Program = typeof(AppAdmin.Program);
        var conn = configuration.GetConnectionString("DefaultConnection");

        services.AddMarsHostInfrastructure(configuration);


        // https://source.dot.net/#Microsoft.AspNetCore.Identity.EntityFrameworkCore/IdentityEntityFrameworkBuilderExtensions.cs,90
        // services.TryAddScoped(typeof(IUserStore<>).MakeGenericType(userType), userStoreType);

        services.AddScoped<IUserClaimsPrincipalFactory<UserEntity>, AppClaimsPrincipalFactory>();


        var jwtSettings = configuration.GetSection(JwtSettings.JwtSectionKey);
        TokenService.ThrowIfJwtProblem(jwtSettings["securityKey"]!);
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.JwtSectionKey));

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
            .AddCookie(cfg =>
            {
                cfg.SlidingExpiration = true;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = jwtSettings["ValidIssuer"],
                    ValidAudience = jwtSettings["ValidAudience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["securityKey"])),
                };

            });
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.SameSite = SameSiteMode.Unspecified;
            options.Cookie.HttpOnly = false;
            int expInMinutes = int.Parse(jwtSettings["expiryInMinutes"]!);
            options.ExpireTimeSpan = TimeSpan.FromMinutes(expInMinutes);

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

    //public static WebApplication MarsUseCore(this WebApplication app)
    //{

    //    return app;
    //}

}
