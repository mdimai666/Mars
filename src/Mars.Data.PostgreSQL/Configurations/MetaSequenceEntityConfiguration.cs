using Mars.Host.Data.Entities;
using Mars.Host.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mars.Host.Data.PostgreSQL.Configurations;

public class MetaSequenceEntityConfiguration : IEntityTypeConfiguration<MetaSequenceEntity>
{
    public void Configure(EntityTypeBuilder<MetaSequenceEntity> entity)
    {
        entity.ToTable("meta_sequences");

        entity.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()")
            .IgnorePropertyFromUpdate();

        entity.Property(x => x.ScopeKey).HasColumnType("varchar(255)").HasMaxLength(255);

        // защита от потерянного инкремента при параллельном создании постов
        entity.Property(x => x.LastValue).IsConcurrencyToken();

        entity.HasOne(x => x.MetaField)
            .WithMany()
            .HasForeignKey(x => x.MetaFieldId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(x => new { x.MetaFieldId, x.ScopeKey }).IsUnique();
    }
}
