using Mars.Host.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Mars.Host.Data.Constants.EntityDefaultConstants;

namespace Mars.Host.Data.InMemory.Configurations;

public class UserLoginEntityConfiguration : IEntityTypeConfiguration<UserLoginEntity>
{
    public void Configure(EntityTypeBuilder<UserLoginEntity> entity)
    {
        entity.ToTable("user_logins");

        entity.Property(x => x.LoginProvider).HasColumnType($"varchar({DefaultNameMaxLength})");
        entity.Property(x => x.ProviderKey).HasColumnType($"varchar({DefaultNameMaxLength})");
        entity.Property(x => x.ProviderDisplayName).HasColumnType($"varchar({DefaultNameMaxLength})");
    }
}
