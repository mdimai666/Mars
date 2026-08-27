using Mars.Host.Data.Entities;
using Mars.Host.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Mars.Host.Data.Constants.EntityDefaultConstants;

namespace Mars.Host.Data.InMemory.Configurations;

public class RoleEntityConfiguration : IEntityTypeConfiguration<RoleEntity>
{
    public void Configure(EntityTypeBuilder<RoleEntity> entity)
    {
        entity.ToTable("roles");

        entity.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()")
            .IgnorePropertyFromUpdate();

        entity.Property(x => x.Name).HasColumnType($"varchar({DefaultNameMaxLength})");
        entity.Property(x => x.NormalizedName).HasColumnType($"varchar({DefaultNameMaxLength})");
        entity.Property(x => x.ConcurrencyStamp).HasColumnType($"varchar({DefaultHashMaxLength})");

        // Each Role can have many entries in the UserRole join table
        entity.HasMany(e => e.UserRoles)
            .WithOne(e => e.Role)
            .HasForeignKey(ur => ur.RoleId)
            .IsRequired();

        // Each Role can have many associated RoleClaims
        entity.HasMany(e => e.RoleClaims)
            .WithOne(e => e.Role)
            .HasForeignKey(rc => rc.RoleId)
            .IsRequired();
    }
}
