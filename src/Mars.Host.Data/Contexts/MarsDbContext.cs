using System.Diagnostics;
using Mars.Core.Exceptions;
using Mars.Host.Data.Contexts.Abstractions;
using Mars.Host.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.Data.Contexts;

public partial class MarsDbContext : IdentityDbContext<UserEntity, RoleEntity, Guid,
                UserClaimEntity, UserRoleEntity, UserLoginEntity, RoleClaimEntity, UserTokenEntity, UserPasskeyEntity>
{
    private readonly DbContextOptions<MarsDbContext> _options;
    public bool IsPooled { get; }

    //--------Asp.Net defaults----------

    public override DbSet<UserEntity> Users { get; set; } = default!;
    public virtual DbSet<UserTypeEntity> UserTypes { get; set; } = default!;
    public override DbSet<RoleEntity> Roles { get; set; } = default!;
    public override DbSet<UserClaimEntity> UserClaims { get; set; } = default!;
    public override DbSet<UserRoleEntity> UserRoles { get; set; } = default!;
    public override DbSet<UserLoginEntity> UserLogins { get; set; } = default!;
    public override DbSet<RoleClaimEntity> RoleClaims { get; set; } = default!;
    public override DbSet<UserTokenEntity> UserTokens { get; set; } = default!;
    public override DbSet<UserPasskeyEntity> UserPasskeys { get; set; } = default!; //TODO: Реализовать, пока не используется.

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
    public virtual DbSet<PostTypePresentationEntity> PostTypePresentations { get; set; } = default!;

    //--------USER----------
    public virtual DbSet<UserTypeMetaFieldEntity> UserTypeMetaFields { get; set; } = default!;
    public virtual DbSet<UserMetaValueEntity> UserMetaValues { get; set; } = default!;

    //--------X----------
    public virtual DbSet<NavMenuEntity> NavMenus { get; set; } = default!;
    public virtual DbSet<FeedbackEntity> Feedbacks { get; set; } = default!;

    public MarsDbContext(DbContextOptions<MarsDbContext> options) : base(options)
    {
        _options = options;
#if DEBUG
        Console.WriteLine($"new {GetType().Name}()");
#endif
        SaveChangesFailed += MarsDbContext_SaveChangesFailed;

        var infra = this.GetInfrastructure();
        //var type = typeof(IDbContextFactory<>).MakeGenericType(GetType());
        IsPooled = infra.GetService<IDbContextFactory<MarsDbContext>>() != null;
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

        //ApplyConfigurationsFromAssembly
        var extension = _options.GetExtension<MarsDbContextOptionExtension>();
        var factory = extension.Factory;
        factory.OnModelCreating(builder);

        bool isPluginInherit = typeof(PluginDbContextBase).IsAssignableFrom(GetType());
        if (isPluginInherit)
        {
#if DEBUG
            Console.WriteLine($"PLUGIN>>EF+init>{GetType().Name}");
#endif
        }

        OnModelCreatingPartial(builder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

    public void ChangeTrackerClearIfPooled()
    {
        if (IsPooled) ChangeTracker.Clear();
    }
}
