using Microsoft.AspNetCore.Identity;

namespace Mars.Data.Entities;

public class UserClaimEntity : IdentityUserClaim<Guid>
{
    public virtual UserEntity? User { get; set; }
}
