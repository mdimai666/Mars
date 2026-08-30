using Mars.Data.Entities;
using Mars.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Mars.Data.Constants.PostConstants;

namespace Mars.Data.PostgreSQL.Configurations;

public class PostStatusEntityConfiguration : IEntityTypeConfiguration<PostStatusEntity>
{
    public void Configure(EntityTypeBuilder<PostStatusEntity> entity)
    {
        entity.ToTable("post_statuses");

        entity.Property(e => e.CreatedAt)
           .HasDefaultValueSql("now()")
           .IgnorePropertyFromUpdate();

        entity.Property(x => x.Title).HasColumnType("text");
        entity.Property(x => x.Slug).HasColumnType($"varchar({StatusMaxLength})");
        entity.Property(x => x.Color).HasColumnType("varchar(50)");

        entity.HasIndex(x => new { x.PostTypeId, x.Slug }).IsUnique();

        // Relations

        entity.HasOne(x => x.PostType)
              .WithMany(x => x.Statuses)
              .HasForeignKey(x => x.PostTypeId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}
