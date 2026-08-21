using Mars.Host.Data.Constants;
using Mars.Host.Data.Entities;
using Mars.Host.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Mars.Host.Data.Constants.PostTypeConstants;

namespace Mars.Host.Data.PostgreSQL.Configurations;

public class MetaFieldEntityConfiguration : IEntityTypeConfiguration<MetaFieldEntity>
{
    public void Configure(EntityTypeBuilder<MetaFieldEntity> entity)
    {
        entity.ToTable("meta_fields");

        entity.Property(e => e.CreatedAt)
           .HasDefaultValueSql("now()")
           .IgnorePropertyFromUpdate();

        entity.Property(x => x.Title).HasColumnType($"varchar({TypeNameMaxLength})").HasMaxLength(TypeNameMaxLength);
        entity.Property(x => x.Key).HasColumnType($"varchar({TypeNameMaxLength})").HasMaxLength(TypeNameMaxLength);
        entity.Property(x => x.Description).HasColumnType($"text").HasMaxLength(TitleMaxLength);
        entity.Property(x => x.Tags).HasColumnType($"character varying({EntityDefaultConstants.TagMaxLength})[]");
        entity.Property(x => x.ModelName).HasColumnType($"varchar({TypeNameMaxLength})").HasMaxLength(TypeNameMaxLength);

        // https://www.npgsql.org/efcore/mapping/json.html?tabs=data-annotations%2Cjsondocument#tojson-owned-entity-mapping
        entity.OwnsMany(x => x.Variants, f => { f.ToJson(); });
        entity.OwnsOne(x => x.Default, f => { f.ToJson(); });
        entity.Property(x => x.Options).HasColumnType("jsonb");

        // Relations
        // Поле принадлежит ровно одному типу (1:N); каскад: тип -> его поля -> значения.

        entity.HasOne(x => x.PostType)
            .WithMany(x => x.MetaFields)
            .HasForeignKey(x => x.PostTypeId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(x => new { x.PostTypeId, x.Key }).IsUnique();

        entity.HasOne(x => x.UserType)
            .WithMany(x => x.MetaFields)
            .HasForeignKey(x => x.UserTypeId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(x => new { x.UserTypeId, x.Key }).IsUnique();

        entity.HasOne(x => x.PostCategoryType)
            .WithMany(x => x.MetaFields)
            .HasForeignKey(x => x.PostCategoryTypeId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(x => new { x.PostCategoryTypeId, x.Key }).IsUnique();

    }
}
