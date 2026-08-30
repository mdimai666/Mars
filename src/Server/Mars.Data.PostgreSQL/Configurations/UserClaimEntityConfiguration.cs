using Mars.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Mars.Data.Constants.EntityDefaultConstants;

namespace Mars.Data.PostgreSQL.Configurations;

public class UserClaimEntityConfiguration : IEntityTypeConfiguration<UserClaimEntity>
{
    public void Configure(EntityTypeBuilder<UserClaimEntity> entity)
    {
        entity.ToTable("user_claims");

        entity.Property(x => x.ClaimType).HasColumnType($"varchar({DefaultShortValueMaxLength})");
        entity.Property(x => x.ClaimValue).HasColumnType($"varchar({DefaultShortValueMaxLength})");
    }
}
