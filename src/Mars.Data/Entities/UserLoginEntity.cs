using Microsoft.AspNetCore.Identity;

namespace Mars.Host.Data.Entities;

public class UserLoginEntity : IdentityUserLogin<Guid>
{
    public virtual UserEntity? User { get; set; }
}
