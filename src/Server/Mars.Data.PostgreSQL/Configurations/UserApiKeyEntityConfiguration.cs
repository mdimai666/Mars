using Mars.Data.Entities;
using Mars.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Mars.Data.Constants.EntityDefaultConstants;

namespace Mars.Data.PostgreSQL.Configurations;

public class UserApiKeyEntityConfiguration : IEntityTypeConfiguration<UserApiKeyEntity>
{
    public void Configure(EntityTypeBuilder<UserApiKeyEntity> entity)
    {
        entity.ToTable("user_api_keys");

        entity.Property(e => e.CreatedAt)
           .HasDefaultValueSql("now()")
           .IgnorePropertyFromUpdate();

        entity.Property(x => x.Name).HasColumnType($"varchar({DefaultNameMaxLength})").IsRequired();
        entity.Property(x => x.KeyHash).HasColumnType($"varchar({DefaultHashMaxLength})").IsRequired();
        entity.Property(x => x.KeyPrefix).HasColumnType($"varchar({DefaultShortValueMaxLength})").IsRequired();

        entity.HasIndex(x => x.UserId);

        entity.HasOne(x => x.User)
            .WithMany(u => u.UserApiKeys)
            .HasForeignKey(x => x.UserId)
            .IsRequired();
    }
}
