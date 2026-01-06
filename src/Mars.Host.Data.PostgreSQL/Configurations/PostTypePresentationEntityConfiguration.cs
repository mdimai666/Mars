using Mars.Host.Data.Entities;
using Mars.Host.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Mars.Host.Data.Constants.PostTypeConstants;

namespace Mars.Host.Data.PostgreSQL.Configurations;

public class PostTypePresentationEntityConfiguration : IEntityTypeConfiguration<PostTypePresentationEntity>
{
    public void Configure(EntityTypeBuilder<PostTypePresentationEntity> entity)
    {
        entity.ToTable("post_type_presentations");

        entity.Property(x => x.ListViewTemplateSourceUri).HasColumnType($"varchar({SourceUriMaxLength})");

    }
}
