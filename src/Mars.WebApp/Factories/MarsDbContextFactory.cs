using Mars.Core.Extensions;
using Mars.Host.Data.Constants;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Options;
using Mars.Host.Data.PostgreSQL;
using Mars.UseStartup;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

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

        var factory = new MarsDbContextPostgreSQLFactory(new DatabaseConnectionOpt
        {
            ConnectionString = connectionString,
            ProviderName = DatabaseProviderConstants.PostgreSQL
        });

        var optionsBuilder = new DbContextOptionsBuilder<MarsDbContext>();
        factory.OptionsBuilderAction(optionsBuilder);

        return new MarsDbContext(optionsBuilder.Options);
    }
}
