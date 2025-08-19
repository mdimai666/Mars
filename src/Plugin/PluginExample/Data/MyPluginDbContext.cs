using Mars.Host.Data.Contexts.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using PluginExample.Data.Entities;

namespace PluginExample.Data;

public partial class MyPluginDbContext : PluginDbContextBase
{
    public override string SchemaName => PluginExamplePlugin.PluginNameFullName;

    public virtual DbSet<PluginNewsEntity> News { get; set; } = default!;

    public MyPluginDbContext(DbContextOptions options) : base(options)
    {

    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        if (optionsBuilder.Options.ContextType != typeof(MyPluginDbContext))
            throw new ArgumentException($"optionsBuilder.Options.ContextType must be '{nameof(MyPluginDbContext)}'. Given '{optionsBuilder.Options.ContextType}'");
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(this.GetType().Assembly);

        OnModelCreatingPartial(builder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

    public static new MyPluginDbContext CreateInstance(string connectionString)
    {
        var builder = new DbContextOptionsBuilder<MyPluginDbContext>();

        builder.UseNpgsql(connectionString);
        builder.UseSnakeCaseNamingConvention();
        builder.EnableDetailedErrors();

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
