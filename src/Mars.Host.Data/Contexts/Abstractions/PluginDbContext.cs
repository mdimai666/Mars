using System.Diagnostics;
using Mars.Core.Exceptions;
using Mars.Host.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Mars.Host.Data.Contexts.Abstractions;

/* see ideas
 https://github.com/dotnet/efcore/issues/26348#issuecomment-1173790018
 */

public abstract class PluginDbContextBase : IdentityDbContext<UserEntity, RoleEntity, Guid,
                UserClaimEntity, UserRoleEntity, UserLoginEntity, RoleClaimEntity, UserTokenEntity>//, IMarsDbContext
{
    public const string PluginEFMigrationsHistoryTable = "__PluginEFMigrationsHistory";

    /// <summary>
    /// each plugin will be placed in a separate scheme by name
    /// <para />
    /// should be - <b>CompanyName.PluginName</b>
    /// </summary>
    public abstract string SchemaName { get; }

    #region baseDbContext
    private readonly DbContextOptions _options;
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
    public virtual DbSet<UserTypeMetaFieldEntity> UserTypeMetaFields { get; set; } = default!;
    public virtual DbSet<UserMetaValueEntity> UserMetaValues { get; set; } = default!;

    //--------X----------
    public virtual DbSet<NavMenuEntity> NavMenus { get; set; } = default!;
    public virtual DbSet<FeedbackEntity> Feedbacks { get; set; } = default!;
    #endregion

    public PluginDbContextBase(DbContextOptions options) : base(options)
    {
        if (string.IsNullOrWhiteSpace(SchemaName))
            throw new ArgumentNullException("SchemaName name must be set");

        _options = options;
#if DEBUG
        Console.WriteLine($"new {GetType().Name}()");
#endif
        SaveChangesFailed += MarsDbContext_SaveChangesFailed;

        var infra = this.GetInfrastructure();
        var type = typeof(IDbContextFactory<>).MakeGenericType(GetType());
        IsPooled = infra.GetService(type) != null;
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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

    }

    public static IEnumerable<Type> ListMarsAllEntities()
    {
        var entitiesNamespace = typeof(PostEntity).Namespace!;
        return typeof(PostEntity).Assembly.GetTypes().Where(t => t.Namespace?.StartsWith(entitiesNamespace) ?? false);
    }

    public static Dictionary<string, Type> GetMarsDbContextDbSetList()
    {
        var dict = new Dictionary<string, Type>();
        var memberDbSets = typeof(MarsDbContext).GetProperties()
                .Where(p => p.PropertyType.IsGenericType
                            && (p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>)
                                || p.PropertyType.GetGenericTypeDefinition() == typeof(List<>)));

        //Console.WriteLine(">memberDbSets length=" + memberDbSets.Count());

        foreach (var prop in memberDbSets)
        {
            //Console.WriteLine("prop=" + prop.Name);
            Type type = prop.PropertyType;
            Type modelType = type.GenericTypeArguments.First();

            dict.Add(prop.Name, modelType);

        }

        return dict;
    }

    //public virtual string Plugin

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(SchemaName);

        //ApplyConfigurationsFromAssembly
        var extension = _options.GetExtension<MarsDbContextOptionExtension>();
        var factory = extension.Factory;
        factory.OnModelCreating(builder);

#if VARIANT_1
        var internalTypes = GetMarsDbContextDbSetList();

        foreach (var internalType in internalTypes)
        {
            Type entityType = internalType.Value;

            string entityTableName = builder.Model.FindEntityType(entityType).GetTableName()!;

            builder.Entity(entityType).ToTable(entityTableName, "public", a => a.ExcludeFromMigrations());

            //builder.Ignore(entityType);
        }
#endif

#if !VARIANT_1
        var internalTypes = ListMarsAllEntities();

        foreach (var internalType in internalTypes)
        {
            Type entityType = internalType;

            string? entityTableName = builder.Model.FindEntityType(entityType)?.GetTableName();
            if (entityTableName == null) continue;

            builder.Entity(entityType).ToTable(entityTableName, "public", a => a.ExcludeFromMigrations());

            //builder.Ignore(entityType);
        }
#endif

        //OnModelCreatingPartial(builder);
    }

    //partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

}
