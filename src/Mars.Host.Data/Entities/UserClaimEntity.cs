using Mars.Host.Data.Configurations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Data.Entities;

[EntityTypeConfiguration(typeof(UserClaimEntityConfiguration))]
public class UserClaimEntity : IdentityUserClaim<Guid>
{
    public virtual UserEntity? User { get; set; }
}
