using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PluginExample.Data.Entities;
using static PluginExample.Data.Constants.PluginNewsConstants;

namespace PluginExample.Data.Configurations;

public class PluginNewsEntityConfiguration : IEntityTypeConfiguration<PluginNewsEntity>
{
    public void Configure(EntityTypeBuilder<PluginNewsEntity> entity)
    {
        entity.ToTable("news");

        entity.Property(e => e.CreatedAt)
           .HasDefaultValueSql("now()");

        entity.Property(x => x.Title).HasColumnType($"text").HasMaxLength(MaxTitleLength);
        entity.Property(x => x.Content).HasColumnType("text").HasMaxLength(MaxContentLength);

        // Relations

        //entity.HasMany(x => x.Files)
        //    .WithMany(x => x.Posts)
        //    .UsingEntity<PluginNewsFilesEntity>(
        //        l => l.HasOne(x => x.FileEntity).WithMany(x => x.PostFiles),
        //        r => r.HasOne(x => x.Post).WithMany(x => x.PostFiles),
        //        k => k.HasKey(x => new { x.PostId, x.FileEntityId })
        //    );

    }
}
