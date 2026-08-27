using Mars.Data.Entities;
using Mars.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mars.Data.PostgreSQL.Configurations;

public class PostCategoryMetaValueEntityConfiguration : IEntityTypeConfiguration<PostCategoryMetaValueEntity>
{
    public void Configure(EntityTypeBuilder<PostCategoryMetaValueEntity> entity)
    {
        entity.ToTable("post_category_meta_values");

        entity.Property(e => e.CreatedAt)
           .HasDefaultValueSql("now()")
           .IgnorePropertyFromUpdate();

        entity.HasIndex(e => new { e.MetaFieldId, e.StringShort });

        entity.HasOne(x => x.PostCategory)
            .WithMany(x => x.MetaValues)
            .HasForeignKey(x => x.PostCategoryId);

        entity.HasOne(x => x.MetaField)
            .WithMany(x => x.PostCategoryMetaValues)
            .HasForeignKey(x => x.MetaFieldId);
    }
}
