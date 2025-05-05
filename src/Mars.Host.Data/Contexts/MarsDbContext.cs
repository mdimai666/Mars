using System.Diagnostics;
using Mars.Core.Exceptions;
using Mars.Host.Data.Contexts.Abstractions;
using Mars.Host.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Mars.Host.Data.Contexts;

public partial class MarsDbContext : IdentityDbContext<UserEntity, RoleEntity, Guid,
                UserClaimEntity, UserRoleEntity, UserLoginEntity, RoleClaimEntity, UserTokenEntity>//, IMarsDbContext
{
    //--------Asp.Net defaults----------

    public override DbSet<UserEntity> Users { get; set; } = default!;
    public override DbSet<RoleEntity> Roles { get; set; } = default!;
    public override DbSet<UserClaimEntity> UserClaims { get; set; } = default!;
    public override DbSet<UserRoleEntity> UserRoles { get; set; } = default!;
    public override DbSet<UserLoginEntity> UserLogins { get; set; } = default!;
    public override DbSet<RoleClaimEntity> RoleClaims { get; set; } = default!;
    public override DbSet<UserTokenEntity> UserTokens { get; set; } = default!;

    //--------CORE----------
    public virtual DbSet<OptionEntity> Options { get; set; } = default!;
    public virtual DbSet<FileEntity> Files { get; set; } = default!;

    //--------POST----------
    public virtual DbSet<PostEntity> Posts { get; set; } = default!;
    public virtual DbSet<PostTypeEntity> PostTypes { get; set; } = default!;
    public virtual DbSet<MetaFieldEntity> MetaFields { get; set; } = default!;
    public virtual DbSet<MetaValueEntity> MetaValues { get; set; } = default!;
    public virtual DbSet<PostTypeMetaFieldEntity> PostTypeMetaFields { get; set; } = default!;
    public virtual DbSet<PostMetaValueEntity> PostMetaValues { get; set; } = default!;

    //--------USER----------
    public virtual DbSet<UserMetaFieldEntity> UserMetaFields { get; set; } = default!;
    public virtual DbSet<UserMetaValueEntity> UserMetaValues { get; set; } = default!;

    public virtual DbSet<NavMenuEntity> NavMenus { get; set; } = default!;
    public virtual DbSet<FeedbackEntity> Feedbacks { get; set; } = default!;

    public MarsDbContext(DbContextOptions options) : base(options)
    {
#if DEBUG
        Console.WriteLine($"new {this.GetType().Name}()");
#endif
        this.SaveChangesFailed += MarsDbContext_SaveChangesFailed;
    }

    [DebuggerStepThrough]
    private void MarsDbContext_SaveChangesFailed(object? sender, SaveChangesFailedEventArgs e)
    {
        if (e.Exception is DbUpdateConcurrencyException)
        {
            throw new ExpiredVersionTokenException("VersionToken Expired", e.Exception);
        }
        throw e.Exception;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        //Apply all [EntityTypeConfiguration(Type)] attribute configurations
        builder.ApplyConfigurationsFromAssembly(this.GetType().Assembly);

        builder.UseSerialColumns();

        bool isPluginInherit = typeof(PluginDbContextBase).IsAssignableFrom(this.GetType());
        if (isPluginInherit)
        {
#if DEBUG
            Console.WriteLine($"PLUGIN>>EF+init>{this.GetType().Name}"); 
#endif
        }

        OnModelCreatingPartial(builder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

    public static MarsDbContext CreateInstance(string connectionString)
    {
        //string connectionString = IOptionService.Configuration.GetConnectionString("DefaultConnection");
        var optionsBuilder = new DbContextOptionsBuilder<MarsDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        optionsBuilder.UseSnakeCaseNamingConvention();
        optionsBuilder.EnableDetailedErrors();
#pragma warning disable CS0618 // Type or member is obsolete
        NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();
#pragma warning restore CS0618 // Type or member is obsolete
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        return new MarsDbContext(optionsBuilder.Options);

    }
}
