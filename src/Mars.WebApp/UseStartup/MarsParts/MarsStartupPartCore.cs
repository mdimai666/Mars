using System.IO.Compression;
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
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
            .AddJwtBearer(); ;

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IKeyMaterialService, IOptions<JwtSettings>, IOptionService>((options, keys, jwtSettings, ops) =>
            {
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
                        if (issuer == ops.SysOption.SiteUrl)
                            return issuer;
                        throw new SecurityTokenInvalidIssuerException($"Invalid issuer: {issuer}");
                    },
                };
            });

        services.ConfigureApplicationCookie(options =>
        {
            //options.Cookie.SameSite = SameSiteMode.Unspecified;
            //options.Cookie.HttpOnly = false;
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

    public static IServiceCollection AddAspNetTools(this IServiceCollection services)
    {
        services.AddDateOnlyTimeOnlyStringConverters()
                .AddResponseCaching()
                .AddMemoryCache(options =>
                {
                    options.TrackStatistics = true;
                })
                .AddLogging();

        services.AddResponseCompression(opts =>
        {
            opts.Providers.Add<BrotliCompressionProvider>();
            opts.Providers.Add<GzipCompressionProvider>();
            opts.EnableForHttps = true;
            opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]);
        })
            .Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal)
            .Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal);

        services.Configure<KestrelServerOptions>(options =>
        {
            options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // if don't set default value is: 30 MB
        });

        services.Configure<FormOptions>(x =>
        {
            x.MultipartBodyLengthLimit = 2L * 1024 * 1024 * 1024;// 2GB
        });

        return services;
    }

    public static IServiceCollection AddMarsSignalRConfiguration(this IServiceCollection services)
    {
        services
            .AddSignalR(hubOptions =>
            {
                hubOptions.EnableDetailedErrors = true;
                hubOptions.KeepAliveInterval = TimeSpan.FromMinutes(1);
            })
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNamingPolicy = null;
            });

        return services;
    }

}
