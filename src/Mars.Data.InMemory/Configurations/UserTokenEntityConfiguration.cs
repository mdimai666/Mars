using Mars.Host.Data.Constants;
using Mars.Host.Data.Entities;
using Mars.Host.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Mars.Host.Data.Constants.EntityDefaultConstants;

namespace Mars.Host.Data.InMemory.Configurations;

public class UserTokenEntityConfiguration : IEntityTypeConfiguration<UserTokenEntity>
{
    public void Configure(EntityTypeBuilder<UserTokenEntity> entity)
    {
        entity.ToTable("user_tokens");

        entity.Property(e => e.CreatedAt)
            .IgnorePropertyFromUpdate()
            .HasDefaultValueSql("now()");

        entity.Property(x => x.LoginProvider).HasColumnType($"varchar({DefaultNameMaxLength})");
        entity.Property(x => x.Name).HasColumnType($"varchar({UserConstants.DefaultNameMaxLength})");
        entity.Property(x => x.Value).HasColumnType($"text");
    }
}
