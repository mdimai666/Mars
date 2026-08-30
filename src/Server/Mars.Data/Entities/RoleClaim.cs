using Microsoft.AspNetCore.Identity;

namespace Mars.Data.Entities;

public class RoleClaimEntity : IdentityRoleClaim<Guid>
{
    public virtual RoleEntity? Role { get; set; }
}
