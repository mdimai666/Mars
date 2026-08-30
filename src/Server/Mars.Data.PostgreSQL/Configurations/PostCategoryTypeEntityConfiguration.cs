using Mars.Data.Constants;
using Mars.Data.Entities;
using Mars.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Mars.Data.Constants.UserTypeConstants;

namespace Mars.Data.PostgreSQL.Configurations;

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
