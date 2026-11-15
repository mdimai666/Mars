using Mars.Data.Entities;
using Mars.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Mars.Data.Constants.EntityDefaultConstants;

namespace Mars.Data.PostgreSQL.Configurations;

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

        // Без HasDefaultValue: при дефолте БД EF опускает CLR-default (false) из INSERT,
        // и новые строки получили бы дефолт БД. Новые обращения всегда пишутся явным false;
        // историческим строкам true проставляет миграция AddFeedbackIsRead.
        entity.Property(x => x.IsRead);

    }
}
