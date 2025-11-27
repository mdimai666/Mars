using System.Text.Json;
using Mars.Host.Data.Entities;
using Mars.Host.Data.Extensions;
using Mars.Host.Data.OwnedTypes.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Mars.Host.Data.Constants.FileConstants;

namespace Mars.Host.Data.InMemory.Configurations;

public class FileEntityConfiguration : IEntityTypeConfiguration<FileEntity>
{
    public void Configure(EntityTypeBuilder<FileEntity> entity)
    {
        entity.ToTable("files");

        entity.Property(e => e.CreatedAt)
           .HasDefaultValueSql("now()")
           .IgnorePropertyFromUpdate();

        entity.Property(x => x.FileExt).HasColumnType($"varchar({ExtMaxLength})");
        entity.Property(x => x.FileName).HasColumnType($"varchar({NameMaxLength})");
        entity.Property(x => x.FilePhysicalPath).HasColumnType("text").HasMaxLength(PathMaxLength);
        entity.Property(x => x.FileVirtualPath).HasColumnType("text").HasMaxLength(PathMaxLength);

        //entity.Property(x => x.Meta).HasColumnType("jsonb");
        //entity.OwnsOne(x => x.Meta);

        //entity.Property(b => b.Meta)
        //    .HasConversion(
        //        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
        //        v => string.IsNullOrEmpty(v)
        //            ? new FileEntityMeta()
        //            : JsonSerializer.Deserialize<FileEntityMeta>(v, (JsonSerializerOptions)null!) ?? new FileEntityMeta()
        //);

        entity.Property(b => b.Meta).HasJsonConversion();
    }
}
