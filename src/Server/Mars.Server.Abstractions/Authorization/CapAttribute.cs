using Microsoft.AspNetCore.Authorization;

namespace Mars.Server.Abstractions.Authorization;

/// <summary>
/// Гейт по гранулярному праву из каталога RoleCaps (ai/RolesPermissionsPlan.md).
/// Политика "Cap:&lt;право&gt;" строится CapPolicyProvider (Mars.Identity.Host) и проверяется
/// через кэш прав ролей, а не клеймы в токене — смена прав действует без релогина.
/// Admin — суперпользователь (bypass в CapAuthorizationHandler).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class CapAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Cap:";

    public CapAttribute(string cap)
    {
        Policy = PolicyPrefix + cap;
    }
}
