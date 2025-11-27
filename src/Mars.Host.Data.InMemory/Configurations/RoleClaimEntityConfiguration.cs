using Mars.Host.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Mars.Host.Data.Constants.EntityDefaultConstants;

namespace Mars.Host.Data.InMemory.Configurations;

public class RoleClaimEntityConfiguration : IEntityTypeConfiguration<RoleClaimEntity>
{
    public void Configure(EntityTypeBuilder<RoleClaimEntity> entity)
    {
        entity.ToTable("role_claims");

        entity.Property(x => x.ClaimType).HasColumnType($"varchar({DefaultShortValueMaxLength})");
        entity.Property(x => x.ClaimValue).HasColumnType($"varchar({DefaultShortValueMaxLength})");

    }
}
