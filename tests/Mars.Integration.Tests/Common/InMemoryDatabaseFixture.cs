using Mars.Host.Data.Constants;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.InMemory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Respawn;

namespace Mars.Integration.Tests.Common;

public class InMemoryDatabaseFixture : IDatabaseFixture
{
    public DbContextOptions<MarsDbContext> _dbContextOptions = default!;
    private Respawner? _respawner;
    MarsDbContext? _dbContext;
    public MarsDbContext DbContext => _dbContext ??= new MarsDbContext(_dbContextOptions);

    protected string? SkipTest => TestConstants.SkipTest;

    public string ConnectionString { get; } = $"{DatabaseProviderConstants.InMemoryDb}:{Guid.NewGuid()}";

    public InMemoryDatabaseFixture()
    {
        if (SkipTest is not null)
        {
            return;
        }

        var path = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");

        var configuration = new ConfigurationBuilder()
            .AddJsonFile(path, optional: true)
            .Build();

        configuration.Providers.First().Set("ConnectionStrings:DefaultConnection", ConnectionString);

        _dbContextOptions = CreateOptions<MarsDbContext>(configuration);
    }

    public Task InitializeAsync()
    {
        if (SkipTest is not null)
        {
            return Task.CompletedTask;
        }

        DbContext.Database.EnsureDeleted();
        DbContext.Database.EnsureCreated();

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    public Task Reset()
    {
        DbContext.Database.EnsureDeleted();
        DbContext.Database.EnsureCreated();
        return Task.CompletedTask;
    }

    private DbContextOptions<TDbContext> CreateOptions<TDbContext>(IConfiguration configuration)
        where TDbContext : DbContext
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;
        var optionsBuilder = new DbContextOptionsBuilder<TDbContext>();
        var factory = new MarsDbContextInMemoryFactory(new() { ConnectionString = connectionString, ProviderName = DatabaseProviderConstants.InMemoryDb });
        factory.OptionsBuilderAction(optionsBuilder);

        return optionsBuilder.EnableSensitiveDataLogging()
                            .EnableDetailedErrors()
                            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                            .Options;
    }

}
