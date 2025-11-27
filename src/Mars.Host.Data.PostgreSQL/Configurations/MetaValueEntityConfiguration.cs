using Mars.Host.Data.Entities;
using Mars.Host.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mars.Host.Data.PostgreSQL.Configurations;

public class MetaValueEntityConfiguration : IEntityTypeConfiguration<MetaValueEntity>
{
    public void Configure(EntityTypeBuilder<MetaValueEntity> entity)
    {
        entity.ToTable("meta_values");

        entity.Property(e => e.CreatedAt)
           .HasDefaultValueSql("now()")
           .IgnorePropertyFromUpdate();


        entity.HasMany(x => x.Posts)
            .WithMany(x => x.MetaValues)
            .UsingEntity<PostMetaValueEntity>(
                l => l.HasOne(x => x.Post).WithMany(x => x.PostMetaValues),
                r => r.HasOne(x => x.MetaValue).WithMany(x => x.PostMetaValues),
                k => k.HasKey(x => new { x.PostId, x.MetaValueId })
            );
        entity.HasMany(x => x.Users)
            .WithMany(x => x.MetaValues)
            .UsingEntity<UserMetaValueEntity>(
                l => l.HasOne(x => x.User).WithMany(x => x.UserMetaValues),
                r => r.HasOne(x => x.MetaValue).WithMany(x => x.UserMetaValues),
                k => k.HasKey(x => new { x.UserId, x.MetaValueId })
            );
    }
}
