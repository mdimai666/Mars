using Microsoft.AspNetCore.Identity;

namespace Mars.Host.Data.Entities;

public class UserRoleEntity : IdentityUserRole<Guid>
{
    public virtual UserEntity? User { get; set; }
    public virtual RoleEntity? Role { get; set; }
}
