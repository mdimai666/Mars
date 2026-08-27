using Mars.Host.Data.Constants;
using Mars.Host.Data.Entities;
using Mars.Host.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Mars.Host.Data.Constants.UserConstants;

namespace Mars.Host.Data.InMemory.Configurations;

public class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> entity)
    {
        entity.ToTable("users");

        entity.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()")
            .IgnorePropertyFromUpdate();

        entity.Property(x => x.FirstName).HasColumnType($"varchar({DefaultNameMaxLength})");
        entity.Property(x => x.LastName).HasColumnType($"varchar({DefaultNameMaxLength})");
        entity.Property(x => x.MiddleName).HasColumnType($"varchar({DefaultNameMaxLength})");

        entity.Property(x => x.PasswordHash).HasColumnType($"varchar({EntityDefaultConstants.DefaultHashMaxLength})");
        entity.Property(x => x.SecurityStamp).HasColumnType($"varchar({EntityDefaultConstants.DefaultHashMaxLength})");
        entity.Property(x => x.ConcurrencyStamp).HasColumnType($"varchar({EntityDefaultConstants.DefaultHashMaxLength})");
        entity.Property(x => x.PhoneNumber).HasColumnType($"varchar({PhoneNumberMaxLength})");
        entity.Property(x => x.AvatarUrl).HasColumnType($"varchar({512})");

        entity.HasMany(s => s.Files)
            .WithOne(s => s.User)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasMany(s => s.Roles)
            .WithMany(s => s.Users)
            .UsingEntity<UserRoleEntity>(
                l => l.HasOne(x => x.Role).WithMany(x => x.UserRoles),
                r => r.HasOne(x => x.User).WithMany(x => x.UserRoles),
                k => k.HasKey(x => new { x.RoleId, x.UserId })
            );

        // https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-9.0#add-all-navigation-properties

        // Each User can have many UserClaims
        entity.HasMany(e => e.Claims)
            .WithOne(e => e.User)
            .HasForeignKey(uc => uc.UserId)
            .IsRequired();

        // Each User can have many UserLogins
        entity.HasMany(e => e.Logins)
            .WithOne(e => e.User)
            .HasForeignKey(ul => ul.UserId)
            .IsRequired();

        // Each User can have many UserTokens
        entity.HasMany(e => e.Tokens)
            .WithOne(e => e.User)
            .HasForeignKey(ut => ut.UserId)
            .IsRequired();

        // Each User can have many entries in the UserRole join table
        entity.HasMany(e => e.UserRoles)
            .WithOne(e => e.User)
            .HasForeignKey(ur => ur.UserId)
            .IsRequired();

        // Each User can have many entries in the UserPasskey join table
        entity.HasMany(e => e.UserPasskeys)
            .WithOne(e => e.User)
            .HasForeignKey(ur => ur.UserId)
            .IsRequired();
    }
}
