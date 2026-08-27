using Mars.Host.Data.Constants;
using Mars.Host.Data.Entities;
using Mars.Host.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Mars.Host.Data.Constants.UserTypeConstants;

namespace Mars.Host.Data.InMemory.Configurations;

public class UserTypeEntityConfiguration : IEntityTypeConfiguration<UserTypeEntity>
{
    public void Configure(EntityTypeBuilder<UserTypeEntity> entity)
    {
        entity.ToTable("user_types");

        entity.Property(e => e.CreatedAt)
           .HasDefaultValueSql("now()")
           .IgnorePropertyFromUpdate();

        entity.Property(x => x.Title).HasColumnType($"text").HasMaxLength(TitleMaxLength);
        entity.Property(x => x.TypeName).HasColumnType($"varchar({TypeNameMaxLength})");
        entity.Property(x => x.Tags).HasColumnType($"character varying({EntityDefaultConstants.TagMaxLength})[]");

        //entity.HasIndex(x => x.TypeName)
        //    .HasFilter("\"disabled\" IS true"); ;

        // Relations

    }
}
