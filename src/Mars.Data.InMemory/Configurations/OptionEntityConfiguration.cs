using Mars.Data.Constants;
using Mars.Data.Entities;
using Mars.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Mars.Data.Constants.OptionConstants;

namespace Mars.Data.InMemory.Configurations;

public class OptionEntityConfiguration : IEntityTypeConfiguration<OptionEntity>
{
    public void Configure(EntityTypeBuilder<OptionEntity> entity)
    {
        entity.ToTable("options");

        entity.Property(e => e.CreatedAt)
            .IgnorePropertyFromUpdate()
            .HasDefaultValueSql("now()");

        entity.Property(x => x.Key).HasColumnType($"varchar({OptionKeyMaxLength})");
        entity.Property(x => x.Type).HasColumnType($"varchar({OptionTypeMaxLength})");

        entity.HasIndex(x => x.Key).IsUnique();
    }
}
