using Mars.Host.Data.Configurations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Data.Entities;

[EntityTypeConfiguration(typeof(UserLoginEntityConfiguration))]
public class UserLoginEntity : IdentityUserLogin<Guid>
{
    public virtual UserEntity? User { get; set; }
}
