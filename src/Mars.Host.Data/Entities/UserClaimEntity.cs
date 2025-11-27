using Microsoft.AspNetCore.Identity;

namespace Mars.Host.Data.Entities;

public class UserClaimEntity : IdentityUserClaim<Guid>
{
    public virtual UserEntity? User { get; set; }
}
