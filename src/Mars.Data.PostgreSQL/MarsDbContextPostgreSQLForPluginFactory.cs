using System.Reflection;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Contexts.Abstractions;
using Mars.Host.Data.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;

namespace Mars.Host.Data.PostgreSQL;

public class MarsDbContextPostgreSQLForPluginFactory(DatabaseConnectionOpt connectionOpt, Assembly migrationsAssembly, string schemaName) : IMarsDbContextFactory
{
    public void OptionsBuilderAction(DbContextOptionsBuilder optionsBuilder)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        NpgsqlConnection.GlobalTypeMapper.UseJsonNet();
        NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();
#pragma warning restore CS0618 // Type or member is obsolete
        optionsBuilder.UseNpgsql(connectionOpt.ConnectionString, opt =>
        {
            opt.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            opt.MigrationsAssembly(migrationsAssembly);
            opt.MigrationsHistoryTable(PluginDbContextBase.PluginEFMigrationsHistoryTable, schemaName);
        });
        optionsBuilder.UseSnakeCaseNamingConvention();
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(new MarsDbContextOptionExtension(this));
    }

    public MarsDbContext CreateInstance()
    {
        var optionsBuilder = new DbContextOptionsBuilder<MarsDbContext>();
        OptionsBuilderAction(optionsBuilder);
        return new MarsDbContext(optionsBuilder.Options);
    }

    public void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(GetType().Assembly);
        builder.ApplyConfigurationsFromAssembly(migrationsAssembly);
        builder.UseSerialColumns();
    }

}
