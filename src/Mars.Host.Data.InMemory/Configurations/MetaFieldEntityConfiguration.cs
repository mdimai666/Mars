using Mars.Host.Data.Constants;
using Mars.Host.Data.Entities;
using Mars.Host.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Mars.Host.Data.Constants.PostTypeConstants;

namespace Mars.Host.Data.InMemory.Configurations;

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

        entity.HasMany(x => x.MetaValues)
            .WithOne(x => x.MetaField);

        // Relations

        entity.HasMany(x => x.PostTypes)
            .WithMany(x => x.MetaFields)
            .UsingEntity<PostTypeMetaFieldEntity>(
                l => l.HasOne(x => x.PostType).WithMany(x => x.PostTypeMetaFields),
                r => r.HasOne(x => x.MetaField).WithMany(x => x.PostTypeMetaFields),
                k => k.HasKey(x => new { x.PostTypeId, x.MetaFieldId })
            );

        entity.HasMany(x => x.UserTypes)
            .WithMany(x => x.MetaFields)
            .UsingEntity<UserTypeMetaFieldEntity>(
                l => l.HasOne(x => x.UserType).WithMany(x => x.UserTypeMetaFields),
                r => r.HasOne(x => x.MetaField).WithMany(x => x.UserTypeMetaFields),
                k => k.HasKey(x => new { x.UserTypeId, x.MetaFieldId })
            );

    }
}
