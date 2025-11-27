using Mars.Host.Data.Contexts;
using Mars.Host.Data.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;

namespace Mars.Host.Data.PostgreSQL;

public class MarsDbContextPostgreSQLFactory(DatabaseConnectionOpt connectionOpt) : IMarsDbContextFactory
{
    public void OptionsBuilderAction(DbContextOptionsBuilder optionsBuilder)
    {
        //var optionsBuilder = new DbContextOptionsBuilder<MarsDbContext>();

#pragma warning disable CS0618 // Type or member is obsolete
        NpgsqlConnection.GlobalTypeMapper.UseJsonNet();
        NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();
#pragma warning restore CS0618 // Type or member is obsolete
        optionsBuilder.UseNpgsql(connectionOpt.ConnectionString, opt =>
        {
            opt.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            opt.MigrationsAssembly(GetType().Assembly);
        });
        optionsBuilder.UseSnakeCaseNamingConvention();
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(new MarsDbContextOptionExtension(this));

        //optionsBuilder.EnableDetailedErrors();
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
        builder.UseSerialColumns();
    }

}
