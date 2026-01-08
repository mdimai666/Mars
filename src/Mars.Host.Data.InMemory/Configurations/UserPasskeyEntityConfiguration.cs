using Mars.Host.Data.Entities;
using Mars.Host.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mars.Host.Data.InMemory.Configurations;

public class UserPasskeyEntityConfiguration : IEntityTypeConfiguration<UserPasskeyEntity>
{
    public void Configure(EntityTypeBuilder<UserPasskeyEntity> entity)
    {
        entity.ToTable("user_passkeys");

        entity.Property(e => e.CreatedAt)
           .HasDefaultValueSql("now()")
           .IgnorePropertyFromUpdate();

        entity.HasKey(e => new { e.UserId, e.CredentialId });
        entity.Property(x => x.CredentialId).IsRequired();
        entity.HasIndex(e => e.CredentialId).IsUnique();

        entity.ComplexProperty(e => e.Data);

        entity.HasOne(x => x.User)
            .WithMany(u => u.UserPasskeys)
            .HasForeignKey(x => x.UserId)
            .IsRequired();
    }
}
