using Mars.Host.Data.Configurations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Data.Entities;

[EntityTypeConfiguration(typeof(UserRoleEntityConfiguration))]
public class UserRoleEntity : IdentityUserRole<Guid>
{
    public virtual UserEntity? User { get; set; }
    public virtual RoleEntity? Role { get; set; }
}
