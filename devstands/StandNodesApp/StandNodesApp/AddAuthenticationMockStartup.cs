using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Mars.Shared.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace StandNodesApp;

public static class AddAuthenticationMockStartup
{
    public static void ConfigureAddAuthenticationMockServices(this WebApplicationBuilder builder)
    {
        // Регистрируем кастомную схему по умолчанию
        const string mockScheme = "MockScheme";
        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = mockScheme;
                options.DefaultChallengeScheme = mockScheme;
                options.DefaultScheme = mockScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, MockAdminAuthHandler>("MockScheme", options => { });

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.ExpireTimeSpan = TimeSpan.FromDays(30);
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
    }

    public static void ConfigureAddAuthenticationMock(this IApplicationBuilder app)
    {

    }
}

public class MockAdminAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public MockAdminAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Создаем личность Администратора «на лету» для абсолютно каждого запроса
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "AutoAdmin"),
            new Claim(ClaimTypes.Role, "Admin") // Важно для [Authorize(Roles = "Admin")]
        };

        var identity = new ClaimsIdentity(claims, "MockScheme");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "MockScheme");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
