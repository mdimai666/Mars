using Mars.Data.Entities;
using Mars.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Mars.Data.Constants.FileConstants;

namespace Mars.Data.PostgreSQL.Configurations;

public class MediaFolderEntityConfiguration : IEntityTypeConfiguration<MediaFolderEntity>
{
    public void Configure(EntityTypeBuilder<MediaFolderEntity> entity)
    {
        entity.ToTable("media_folders");

        entity.Property(e => e.CreatedAt)
           .HasDefaultValueSql("now()")
           .IgnorePropertyFromUpdate();

        entity.Property(x => x.Name).HasColumnType($"varchar({NameMaxLength})");
        entity.Property(x => x.Path).HasColumnType("text").HasMaxLength(PathMaxLength);
        entity.Property(x => x.Icon).HasColumnType($"varchar({NameMaxLength})");

        entity.HasIndex(x => x.Path).IsUnique();

        entity.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasMany(x => x.Files)
            .WithOne(x => x.Folder)
            .HasForeignKey(x => x.FolderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
