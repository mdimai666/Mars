using Mars.Host.Data.Constants;
using Mars.Host.Data.Entities;
using Mars.Host.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Mars.Host.Data.Constants.UserTypeConstants;

namespace Mars.Host.Data.PostgreSQL.Configurations;

public class PostCategoryTypeEntityConfiguration : IEntityTypeConfiguration<PostCategoryTypeEntity>
{
    public void Configure(EntityTypeBuilder<PostCategoryTypeEntity> entity)
    {
        entity.ToTable("post_category_types");

        entity.Property(e => e.CreatedAt)
           .HasDefaultValueSql("now()")
           .IgnorePropertyFromUpdate();

        entity.Property(x => x.Title).HasColumnType($"varchar({TitleMaxLength})").HasMaxLength(TitleMaxLength);
        entity.Property(x => x.TypeName).HasColumnType($"varchar({TypeNameMaxLength})");
        entity.Property(x => x.Tags).HasColumnType($"character varying({EntityDefaultConstants.TagMaxLength})[]");

    }
}
