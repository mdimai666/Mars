using Mars.Data.Constants;
using Mars.Data.Contexts;
using Mars.Data.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TestApp.Mars.Data;

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
        var factory = new MarsDbContextPostgreSQLFactory(new() { ConnectionString = connectionString, ProviderName = DatabaseProviderConstants.PostgreSQL });
        factory.OptionsBuilderAction(optionsBuilder);

        optionsBuilder.EnableDetailedErrors(true);
        optionsBuilder.EnableSensitiveDataLogging(true);

        return factory.CreateInstance();
    }
}
