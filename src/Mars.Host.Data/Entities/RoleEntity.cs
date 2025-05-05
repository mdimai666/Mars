using System.ComponentModel.DataAnnotations.Schema;
using Mars.Host.Data.Common;
using Mars.Host.Data.Configurations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Data.Entities;

[EntityTypeConfiguration(typeof(RoleEntityConfiguration))]
public class RoleEntity : IdentityRole<Guid>, IBasicEntity, IActivatableEntity
{
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }


    [NotMapped]
    public virtual List<UserEntity>? Users { get; set; } = default!;
    public virtual ICollection<UserRoleEntity>? UserRoles { get; set; } = default!;

    [NotMapped]
    public virtual List<RoleClaimEntity>? Claims { get; set; } = default!;
    public virtual ICollection<RoleClaimEntity>? RoleClaims { get; set; } = default!;
}
