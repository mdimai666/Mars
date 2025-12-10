using Mars.Host.Data.Constants;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Contexts.Abstractions;
using Mars.Host.Data.Options;
using Mars.Host.Data.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using PluginExample.Data.Entities;

namespace PluginExample.Data;

public partial class MyPluginDbContext : PluginDbContextBase
{
    public override string SchemaName => PluginExamplePlugin.PluginNameFullName;

    public virtual DbSet<PluginNewsEntity> News { get; set; } = default!;

    public MyPluginDbContext(DbContextOptions<MyPluginDbContext> options) : base(options)
    {

    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        var x = optionsBuilder.IsConfigured;

        if (optionsBuilder.Options.ContextType != typeof(MyPluginDbContext))
            throw new ArgumentException($"optionsBuilder.Options.ContextType must be '{nameof(MyPluginDbContext)}'. Given '{optionsBuilder.Options.ContextType}'");
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(GetType().Assembly);

        OnModelCreatingPartial(builder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

    public static MyPluginDbContext CreateInstance(string connectionString)
    {
        var builder = new DbContextOptionsBuilder<MyPluginDbContext>();

        var connectOpt = new DatabaseConnectionOpt() { ConnectionString = connectionString, ProviderName = DatabaseProviderConstants.PostgreSQL };
        var factory = new MarsDbContextPostgreSQLForPluginFactory(connectOpt, typeof(MyPluginDbContext).Assembly, PluginExamplePlugin.PluginNameFullName);
        factory.OptionsBuilderAction(builder);
        ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(new MarsDbContextOptionExtension(factory));

        return new MyPluginDbContext(builder.Options);
    }
}

public class MyPluginDbContextFactory : IDesignTimeDbContextFactory<MyPluginDbContext>
{
    public MyPluginDbContext CreateDbContext(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.DesignTime.json")
                        .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")!;

        return MyPluginDbContext.CreateInstance(connectionString);
    }
}
