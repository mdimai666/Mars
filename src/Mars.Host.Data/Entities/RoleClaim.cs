using Microsoft.AspNetCore.Identity;

namespace Mars.Host.Data.Entities;

public class RoleClaimEntity : IdentityRoleClaim<Guid>
{
    public virtual RoleEntity? Role { get; set; }
}
