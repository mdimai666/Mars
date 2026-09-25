using System.Security.Claims;
using Mars.Data.Entities;
using Mars.Identity.Abstractions.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Identity.Host.Authentication;

/// <summary>
/// Валидация cookie-принципала: сначала быстрый путь через <see cref="ISecurityStampCache"/>
/// (права/блокировка применяются на следующем же запросе после правки), затем штатный
/// SecurityStampValidator (сверка с БД не чаще ValidationInterval).
/// Подключается в ConfigureApplicationCookie (OnValidatePrincipal) — важно, что Events там
/// пересоздаётся целиком и штатная привязка Identity теряется, поэтому внутренний вызов явный.
/// </summary>
public static class CookiePrincipalValidator
{
    // тот же claim type, что кладут UserClaimsPrincipalFactory и TokenService
    private const string SecurityStampClaimType = "AspNet.Identity.SecurityStamp";

    public static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        if (await TryRefreshFromCacheAsync(context))
            return;

        await SecurityStampValidator.ValidateAsync<ISecurityStampValidator>(context);
    }

    private static async Task<bool> TryRefreshFromCacheAsync(CookieValidatePrincipalContext context)
    {
        var services = context.HttpContext.RequestServices;
        var stampCache = services.GetService<ISecurityStampCache>();
        if (stampCache is null || context.Principal is null)
            return false;

        if (!Guid.TryParse(context.Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return false;

        if (!stampCache.TryGetNewStamp(userId, out var newStamp))
            return false;

        var cookieStamp = context.Principal.FindFirstValue(SecurityStampClaimType);
        if (string.Equals(cookieStamp, newStamp, StringComparison.Ordinal))
            return false; // кука уже перевыпущена с новым stamp — ничего не делаем

        var userManager = services.GetRequiredService<UserManager<UserEntity>>();
        var signInManager = services.GetRequiredService<SignInManager<UserEntity>>();
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null || !await signInManager.CanSignInAsync(user))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return true;
        }

        context.ReplacePrincipal(await signInManager.CreateUserPrincipalAsync(user));
        context.ShouldRenew = true;
        return true;
    }
}
