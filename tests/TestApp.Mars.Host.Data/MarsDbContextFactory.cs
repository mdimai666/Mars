using Mars.Host.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace TestApp.Mars.Host.Data;

public class MarsDbContextFactory : IDesignTimeDbContextFactory<MarsDbContext>
{
    public MarsDbContext CreateDbContext(string[] args)
    {
        var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false);

        IConfiguration config = builder.Build();

        string connectionString = config.GetConnectionString("DefaultConnection")!;

        //---

        var optionsBuilder = new DbContextOptionsBuilder<MarsDbContext>();

        optionsBuilder.UseNpgsql(connectionString, o =>
        {
            //o.UseNodaTime();
            //o.SetPostgresVersion(9, 6);
            //o.MigrationsAssembly("Mars.Host"); //non required
            o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
#pragma warning disable CS0618 // Type or member is obsolete
            NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();
#pragma warning restore CS0618 // Type or member is obsolete
        });

        optionsBuilder.UseSnakeCaseNamingConvention();
        optionsBuilder.EnableDetailedErrors(true);
#if DEBUG
        optionsBuilder.EnableSensitiveDataLogging(true);
#endif
        return new MarsDbContext(optionsBuilder.Options);
    }
}
