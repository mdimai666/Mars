using Mars.Host.Data.Entities;
using Mars.Host.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mars.Host.Data.PostgreSQL.Configurations;

public class PostMetaValueEntityConfiguration : IEntityTypeConfiguration<PostMetaValueEntity>
{
    public void Configure(EntityTypeBuilder<PostMetaValueEntity> entity)
    {
        entity.ToTable("post_meta_values");

        entity.Property(e => e.CreatedAt)
           .HasDefaultValueSql("now()")
           .IgnorePropertyFromUpdate();

        entity.HasIndex(e => new { e.MetaFieldId, e.StringShort });

        entity.HasOne(x => x.Post)
            .WithMany(x => x.MetaValues)
            .HasForeignKey(x => x.PostId);

        entity.HasOne(x => x.MetaField)
            .WithMany(x => x.PostMetaValues)
            .HasForeignKey(x => x.MetaFieldId);
    }
}
