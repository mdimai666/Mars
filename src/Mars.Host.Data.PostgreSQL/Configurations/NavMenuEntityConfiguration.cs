using Mars.Host.Data.Entities;
using Mars.Host.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Mars.Host.Data.Constants.EntityDefaultConstants;

namespace Mars.Host.Data.PostgreSQL.Configurations;

public class NavMenuEntityConfiguration : IEntityTypeConfiguration<NavMenuEntity>
{
    public void Configure(EntityTypeBuilder<NavMenuEntity> entity)
    {
        entity.ToTable("nav_menus");

        entity.Property(e => e.CreatedAt)
            .IgnorePropertyFromUpdate()
            .HasDefaultValueSql("now()");

        entity.Property(x => x.Title).HasColumnType($"varchar({DefaultNameMaxLength})");
        entity.Property(x => x.Slug).HasColumnType($"varchar({DefaultShortValueMaxLength})");
        entity.Property(x => x.Class).HasColumnType($"varchar({DefaultShortValueMaxLength})");
        entity.Property(x => x.Style).HasColumnType($"varchar({DefaultShortValueMaxLength})");
        entity.Property(x => x.Tags).HasColumnType($"varchar({TagMaxLength})[]");
        entity.Property(x => x.Roles).HasColumnType($"varchar({TagMaxLength})[]");

        entity.OwnsMany(x => x.MenuItems, f => { f.ToJson(); });

    }
}
