using Mars.Host.Data.Constants;
using Mars.Host.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Mars.Host.Data.Constants.PostConstants;

namespace Mars.Host.Data.PostgreSQL.Configurations;

public class PostCategoryEntityConfiguration : IEntityTypeConfiguration<PostCategoryEntity>
{
    public void Configure(EntityTypeBuilder<PostCategoryEntity> entity)
    {
        entity.ToTable("post_categories");

        entity.Property(e => e.CreatedAt)
           .HasDefaultValueSql("now()");

        entity.Property(x => x.Title).HasColumnType($"varchar({TitleMaxLength})").HasMaxLength(TitleMaxLength);
        entity.Property(x => x.Slug).HasColumnType($"varchar({SlugMaxLength})");
        entity.Property(x => x.Tags).HasColumnType($"character varying({EntityDefaultConstants.TagMaxLength})[]");

        entity.Property(x => x.Path).HasColumnType($"varchar({2048})");
        entity.Property(x => x.SlugPath).HasColumnType($"varchar({2048})");

        entity.HasMany(x => x.Posts)
            .WithMany(x => x.Categories)
            .UsingEntity<PostPostCategoriesEntity>(
                l => l.HasOne(x => x.Post).WithMany(x => x.PostPostCategories),
                r => r.HasOne(x => x.PostCategory).WithMany(x => x.PostPostCategories),
                k => k.HasKey(x => new { x.PostId, x.PostCategoryId })
            );
    }
}
