using Mars.Host.Data.Configurations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Data.Entities;

[EntityTypeConfiguration(typeof(UserTokenEntityConfiguration))]
public class UserTokenEntity : IdentityUserToken<Guid>
{
    [Comment("Создан")]
    public DateTimeOffset CreatedAt { get; set; }

    public virtual UserEntity? User { get; set; }
}
