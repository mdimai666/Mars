using Mars.Host.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Mars.Host.Data.Constants.EntityDefaultConstants;

namespace Mars.Host.Data.InMemory.Configurations;

public class UserClaimEntityConfiguration : IEntityTypeConfiguration<UserClaimEntity>
{
    public void Configure(EntityTypeBuilder<UserClaimEntity> entity)
    {
        entity.ToTable("user_claims");

        entity.Property(x => x.ClaimType).HasColumnType($"varchar({DefaultShortValueMaxLength})");
        entity.Property(x => x.ClaimValue).HasColumnType($"varchar({DefaultShortValueMaxLength})");
    }
}
