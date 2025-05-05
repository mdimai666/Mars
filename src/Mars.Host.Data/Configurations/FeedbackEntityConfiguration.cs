using Mars.Host.Data.Entities;
using Mars.Host.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Mars.Host.Data.Constants.EntityDefaultConstants;

namespace Mars.Host.Data.Configurations;

public class FeedbackEntityConfiguration : IEntityTypeConfiguration<FeedbackEntity>
{
    public void Configure(EntityTypeBuilder<FeedbackEntity> entity)
    {
        entity.ToTable("feedbacks");

        entity.Property(e => e.CreatedAt)
           .HasDefaultValueSql("now()")
           .IgnorePropertyFromUpdate();

        entity.Property(x => x.Title).HasColumnType("text").HasMaxLength(DefaultNameMaxLength);
        entity.Property(x => x.Content).HasColumnType("text").HasMaxLength(DefaultNameMaxLength);
        entity.Property(x => x.FilledUsername).HasColumnType($"varchar({DefaultNameMaxLength})").HasMaxLength(DefaultNameMaxLength);
        entity.Property(x => x.Phone).HasColumnType($"varchar({DefaultShortValueMaxLength})").HasMaxLength(DefaultShortValueMaxLength);
        entity.Property(x => x.Email).HasColumnType($"varchar({DefaultShortValueMaxLength})").HasMaxLength(DefaultShortValueMaxLength);

    }
}
