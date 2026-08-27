using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Data.Entities;

public class UserPasskeyEntity : IdentityUserPasskey<Guid>
{
    [Comment("Создан")]
    public DateTimeOffset CreatedAt { get; set; }

    [Comment("Изменен")]
    public DateTimeOffset? ModifiedAt { get; set; }

    [Comment("Отключен")]
    public bool Disabled { get; set; }

    public virtual UserEntity? User { get; set; }

}
