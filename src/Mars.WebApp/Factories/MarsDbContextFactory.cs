using Mars.Core.Extensions;
using Mars.Host.Data.Contexts;
using Mars.UseStartup;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace Mars.Factories;

public class MarsDbContextFactory : IDesignTimeDbContextFactory<MarsDbContext>
{
    public MarsDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .ConfigureAppConfiguration(args)
            .Build();
        string connectionString = configuration.GetConnectionString("DefaultConnection")!;

#if DEBUG
        Console.WriteLine($"args = {args.JoinStr(" ")}");
        Console.WriteLine($"directory = {Directory.GetCurrentDirectory()}");
        Console.WriteLine("connectionString = " + connectionString);
#endif

        var optionsBuilder = new DbContextOptionsBuilder<MarsDbContext>();

        optionsBuilder.UseNpgsql(connectionString, o =>
        {
            o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
#pragma warning disable CS0618 // Type or member is obsolete
            NpgsqlConnection.GlobalTypeMapper.UseJsonNet();
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
