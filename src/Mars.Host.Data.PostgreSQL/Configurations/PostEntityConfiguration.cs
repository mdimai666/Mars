using Mars.Host.Data.Constants;
using Mars.Host.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Mars.Host.Data.Constants.PostConstants;

namespace Mars.Host.Data.PostgreSQL.Configurations;

public class PostEntityConfiguration : IEntityTypeConfiguration<PostEntity>
{
    public void Configure(EntityTypeBuilder<PostEntity> entity)
    {
        entity.ToTable("posts");

        entity.Property(e => e.CreatedAt)
           .HasDefaultValueSql("now()");

        entity.Property(x => x.Title).HasColumnType($"text").HasMaxLength(TitleMaxLength);
        entity.Property(x => x.Slug).HasColumnType($"varchar({SlugMaxLength})");
        entity.Property(x => x.Tags).HasColumnType($"character varying({EntityDefaultConstants.TagMaxLength})[]");
        entity.Property(x => x.Content).HasColumnType("text");
        entity.Property(x => x.Excerpt).HasColumnType("text").HasMaxLength(ExcerptMaxLength);
        entity.Property(x => x.Status).HasColumnType($"varchar({StatusMaxLength})");

        //entity.Property(x => x.Type).HasColumnType($"varchar({PostTypeConstants.TypeNameMaxLength})");
        entity.Property(x => x.LangCode).HasColumnType($"varchar({LangCodeMaxLength})");

        // Relations

        entity.HasMany(x => x.Files)
            .WithMany(x => x.Posts)
            .UsingEntity<PostFilesEntity>(
                l => l.HasOne(x => x.FileEntity).WithMany(x => x.PostFiles),
                r => r.HasOne(x => x.Post).WithMany(x => x.PostFiles),
                k => k.HasKey(x => new { x.PostId, x.FileEntityId })
            );

        //entity.HasOne(x => x.PostType)
        //      .WithMany(x => x.Posts)
        //      .IsRequired(false)
        //      .OnDelete(DeleteBehavior.Restrict)
        //      .HasForeignKey(post => post.Type)
        //      .HasPrincipalKey(postType => postType.TypeName)
        //      ;

    }
}
