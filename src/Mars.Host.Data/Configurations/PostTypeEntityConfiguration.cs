using Mars.Host.Data.Constants;
using Mars.Host.Data.Entities;
using Mars.Host.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Mars.Host.Data.Constants.PostTypeConstants;

namespace Mars.Host.Data.Configurations;

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

        entity.OwnsMany(x => x.PostStatusList, f => { f.ToJson(); });
        entity.OwnsOne(x => x.PostContentType, f => { f.ToJson(); });

        // Relations

        //entity.HasMany(x => x.MetaFields)
        //        .WithMany(x => x.)
        //        .UsingEntity<PostFilesEntity>();
    }
}
