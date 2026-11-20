using Microsoft.AspNetCore.Authorization;

namespace Mars.Server.Abstractions.Authorization;

/// <summary>
/// Требование политики "Cap:&lt;право&gt;": роли пользователя должны давать право <see cref="Cap"/>.
/// </summary>
public sealed class CapRequirement : IAuthorizationRequirement
{
    public string Cap { get; }

    public CapRequirement(string cap)
    {
        Cap = cap;
    }
}
