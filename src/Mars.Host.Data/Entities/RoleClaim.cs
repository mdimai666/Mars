using Mars.Host.Data.Configurations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Data.Entities;

[EntityTypeConfiguration(typeof(RoleClaimEntityConfiguration))]
public class RoleClaimEntity : IdentityRoleClaim<Guid>
{
    public virtual RoleEntity? Role { get; set; }
}
