using Mars.Data.Entities;
using Mars.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mars.Data.PostgreSQL.Configurations;

public class UserMetaValueEntityConfiguration : IEntityTypeConfiguration<UserMetaValueEntity>
{
    public void Configure(EntityTypeBuilder<UserMetaValueEntity> entity)
    {
        entity.ToTable("user_meta_values");

        entity.Property(e => e.CreatedAt)
           .HasDefaultValueSql("now()")
           .IgnorePropertyFromUpdate();

        entity.HasIndex(e => new { e.MetaFieldId, e.StringShort });

        entity.HasOne(x => x.User)
            .WithMany(x => x.MetaValues)
            .HasForeignKey(x => x.UserId);

        entity.HasOne(x => x.MetaField)
            .WithMany(x => x.UserMetaValues)
            .HasForeignKey(x => x.MetaFieldId);
    }
}
