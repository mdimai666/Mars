using Microsoft.AspNetCore.Identity;

namespace Mars.Data.Entities;

public class UserLoginEntity : IdentityUserLogin<Guid>
{
    public virtual UserEntity? User { get; set; }
}
