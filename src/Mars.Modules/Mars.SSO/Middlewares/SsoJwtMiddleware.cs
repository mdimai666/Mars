using System.Security.Claims;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.SSO.Interfaces;
using Mars.Host.Shared.SSO.Services;
using Mars.SSO.Services;
using Microsoft.AspNetCore.Http;

namespace Mars.SSO.Middlewares;

public class SsoAuthMiddleware
{
    private readonly RequestDelegate _next;

    public SsoAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context,
        ISsoService ssoService,
        IUserService userService,
        ILocalJwtService localJwt,
        ITokenCache tokenCache)
    {
        if (context.Request.Path == "/favicon.ico") goto Next;
        if (context.Request.Method == "OPTIONS") goto Next;
        if (context.Request.Method == "TRACE") goto Next;

        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            goto Next;

        var cancellationToken = context?.RequestAborted ?? CancellationToken.None;

        var token = authHeader["Bearer ".Length..];

        // Сначала попробуем локальный токен
        var principal = localJwt.ValidateToken(token);
        if (principal != null)
        {
            context.User = principal;
            await _next(context);
            return;
        }

        var providers = ssoService.CreateProviderList();

        // Если токен внешний
        foreach (var provider in providers)
        {
            var extPrincipal = await provider.ValidateTokenAsync(token);
            if (extPrincipal == null) continue;

            var extUser = await EnsureUserCreatedFromSsoAsync(provider, extPrincipal, userService, cancellationToken);

            // Проверяем, не выдавали ли уже локальный токен
            var cachedToken = await tokenCache.GetLocalTokenAsync(token);
            if (cachedToken == null)
            {
                var localToken = await localJwt.CreateToken(extUser.Id, cancellationToken);
                await tokenCache.CacheAsync(token, localToken, TimeSpan.FromHours(1));
                //context.Response.Headers.Add("X-New-Local-Token", localToken);
            }

            // Теперь создаём claims и авторизуем
            List<Claim> claims = [
                new Claim(ClaimTypes.NameIdentifier, extUser.Id.ToString()),
                new Claim(ClaimTypes.Name, extUser.UserName),
                //new Claim(ClaimTypes.Email, extUser.Email??""),
                new Claim("provider", provider.Name)
            ];
            if (!string.IsNullOrEmpty(extUser.Email))
                claims.Add(new Claim(ClaimTypes.Email, extUser.Email!));
            foreach(var role in extUser.Roles)
            {
                claims.Add(new(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, provider.Name);
            context.User = new ClaimsPrincipal(identity);

            await _next(context);
            return;
        }

    Next:
        await _next(context!);
    }

    public Task<AuthorizedUserInformationDto> EnsureUserCreatedFromSsoAsync(
                ISsoProvider provider,
                ClaimsPrincipal principal,
                IUserService userService,
                CancellationToken cancellationToken)
    {
        var query = provider.MapToCreateUserQuery(principal);
        return userService.RemoteUserUpsert(query, cancellationToken);
    }
}
