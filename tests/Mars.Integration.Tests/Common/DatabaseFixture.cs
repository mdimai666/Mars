using Mars.Host.Data.Contexts;
using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NSubstitute;
using Respawn;
using Testcontainers.PostgreSql;

namespace Mars.Integration.Tests.Common;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = default!;
    public DbContextOptions _dbContextOptions = default!;
    private Respawner _respawner = default!;

    protected string? SkipTest => TestConstants.SkipTest;

    public string connectionString => _container.GetConnectionString();

    public DatabaseFixture()
    {
        if (SkipTest is not null)
        {
            return;
        }

        _container = new PostgreSqlBuilder()
            .WithName($"mars-test-{Guid.NewGuid()}")
            .WithUsername("postgres1")
            .WithPassword("postgres1")
            .WithDatabase("test_db")
            .WithImage("postgres:14")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready"))
            .WithCleanUp(true)
            .Build();
    }

    public MarsDbContext DbContext => new MarsDbContext(_dbContextOptions);

    public async Task InitializeAsync()
    {
        if (SkipTest is not null)
        {
            return;
        }

        await _container.StartAsync();

        var path = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");

        var configuration = new ConfigurationBuilder().AddJsonFile(path).Build();
        configuration.Providers.First().Set("ConnectionStrings:DefaultConnection", _container.GetConnectionString());

        _dbContextOptions = CreateOptions<MarsDbContext>(configuration, typeof(MarsDbContext).Assembly.FullName!);

        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
#pragma warning disable CS0618 // Type or member is obsolete
        NpgsqlConnection.GlobalTypeMapper.UseJsonNet();
        NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();
#pragma warning restore CS0618 // Type or member is obsolete

        var canConnect = await DbContext.Database.CanConnectAsync();

        if (!canConnect) throw new Exception("cannot connect to Database");

        //DbContext.Database.EnsureCreated();
        //var all1 = DbContext.Database.GetMigrations().ToList();
        //var all2 = DbContext.Database.GetAppliedMigrations().ToList();
        //var all3 = DbContext.Database.GetPendingMigrations().ToList();

        await DbContext.Database.MigrateAsync();

        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(connection,
            new RespawnerOptions { DbAdapter = DbAdapter.Postgres });
    }

    public async Task DisposeAsync()
    {
        if (SkipTest is not null)
        {
            return;
        }

        await _container.DisposeAsync();
    }

    public async Task Reset()
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString() + ";Include Error Detail=True;");
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    private static DbContextOptions<TDbContext> CreateOptions<TDbContext>(IConfiguration configuration, string migrationAssembly) where TDbContext : DbContext =>
        new DbContextOptionsBuilder<TDbContext>()
            .UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(migrationAssembly)
                      .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
            .UseApplicationServiceProvider(GetServiceProviderMock())
            //.ReplaceHistoryRepositoryToPostgres()
            .UseSnakeCaseNamingConvention()
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors()
            //.LogTo(m => Debug.WriteLine(m))
            .Options;

    private static IServiceProvider GetServiceProviderMock()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();

        //serviceProvider.GetService<ISoftDeleteParams>()
        //    .Returns(new PostgresDbContextDefaultValues());

        return serviceProvider;
    }
}
