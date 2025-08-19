using Mars.Host.Data.Configurations;
using Mars.Host.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Data.Contexts.Abstractions;

/* see ideas
 https://github.com/dotnet/efcore/issues/26348#issuecomment-1173790018
 */

public abstract class PluginDbContextBase : MarsDbContext
{
    public const string PluginEFMigrationsHistoryTable = "__PluginEFMigrationsHistory";

    /// <summary>
    /// each plugin will be placed in a separate scheme by name
    /// <para />
    /// should be - <b>CompanyName.PluginName</b>
    /// </summary>
    public abstract string SchemaName { get; }

    public PluginDbContextBase(DbContextOptions options) : base(options)
    {
        if (string.IsNullOrWhiteSpace(SchemaName))
            throw new ArgumentNullException("SchemaName name must be set");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        optionsBuilder.UseNpgsql(opt => opt.MigrationsHistoryTable(PluginEFMigrationsHistoryTable, SchemaName));
        optionsBuilder.UseSnakeCaseNamingConvention();
        optionsBuilder.EnableDetailedErrors();
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
        builder.ApplyConfigurationsFromAssembly(typeof(PostEntityConfiguration).Assembly);

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
