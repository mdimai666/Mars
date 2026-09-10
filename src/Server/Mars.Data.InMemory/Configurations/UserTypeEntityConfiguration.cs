using Mars.Data.Constants;
using Mars.Data.Entities;
using Mars.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Mars.Data.Constants.UserTypeConstants;

namespace Mars.Data.InMemory.Configurations;

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
