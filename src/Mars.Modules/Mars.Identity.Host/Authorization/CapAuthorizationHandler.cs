using System.Security.Claims;
using Mars.Identity.Abstractions.Services;
using Mars.Server.Abstractions.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace Mars.Identity.Host.Authorization;

/// <summary>
/// Роли — из principal (ClaimTypes.Role), права — из кэша прав ролей: смена прав действует
/// сразу, без релогина. Admin — суперпользователь: всегда успешен, защита от локаута
/// при редактировании матрицы прав. Без аутентификации — fail.
/// </summary>
public sealed class CapAuthorizationHandler : AuthorizationHandler<CapRequirement>
{
    private readonly IRoleCapsCache _roleCapsCache;

    public CapAuthorizationHandler(IRoleCapsCache roleCapsCache)
    {
        _roleCapsCache = roleCapsCache;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CapRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true) return;

        var roles = context.User.FindAll(ClaimTypes.Role).Select(s => s.Value).ToList();
        if (roles.Count == 0) return;

        if (roles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
            return;
        }

        var roleCaps = await _roleCapsCache.GetRoleCaps();
        if (roles.Any(role => roleCaps.TryGetValue(role, out var caps) && caps.Contains(requirement.Cap)))
        {
            context.Succeed(requirement);
        }
    }
}
