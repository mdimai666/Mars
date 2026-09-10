using Mars.Data.Constants;
using Mars.Data.Entities;
using Mars.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Mars.Data.Constants.PostTypeConstants;

namespace Mars.Data.InMemory.Configurations;

public class PostTypeEntityConfiguration : IEntityTypeConfiguration<PostTypeEntity>
{
    public void Configure(EntityTypeBuilder<PostTypeEntity> entity)
    {
        entity.ToTable("post_types");

        entity.Property(e => e.CreatedAt)
           .HasDefaultValueSql("now()")
           .IgnorePropertyFromUpdate();

        entity.Property(x => x.Title).HasColumnType($"text").HasMaxLength(TitleMaxLength);
        entity.Property(x => x.TypeName).HasColumnType($"varchar({TypeNameMaxLength})");
        entity.Property(x => x.Tags).HasColumnType($"character varying({EntityDefaultConstants.TagMaxLength})[]");

        entity.HasIndex(x => x.TypeName)
            .HasFilter("\"disabled\" IS true"); ;

        //entity.Property(x => x.EnabledFeatures).HasColumnType("jsonb");
        entity.Property(x => x.Options)
            .HasColumnType("jsonb")
            .HasJsonConversion();

        // Relations

        //entity.HasMany(x => x.MetaFields)
        //        .WithMany(x => x.)
        //        .UsingEntity<PostFilesEntity>();
    }
}
