using Mars.Data.Entities;
using Mars.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Mars.Data.Constants.PostTypeConstants;

namespace Mars.Data.PostgreSQL.Configurations;

public class PostTypePresentationEntityConfiguration : IEntityTypeConfiguration<PostTypePresentationEntity>
{
    public void Configure(EntityTypeBuilder<PostTypePresentationEntity> entity)
    {
        entity.ToTable("post_type_presentations");

        entity.Property(x => x.ListViewTemplateSourceUri).HasColumnType($"varchar({SourceUriMaxLength})");
        entity.Property(x => x.GridSettings).HasColumnType("jsonb");

    }
}
