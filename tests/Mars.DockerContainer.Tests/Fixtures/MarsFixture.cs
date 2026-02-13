using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;

namespace ExternalServices.Integration.Tests.MarsDocker.Fixtures;

public class MarsFixture : IAsyncLifetime
{
    private IContainer _marsContainer = default!;
    private IFlurlClient _client = default!;
    private INetwork _network = default!;
    private const string _networkName = "mars-test-network";

    private PostgreSqlContainer _postgresContainer = default!;
    public string ConnectionString => _postgresContainer.GetConnectionString();
    public string MarsUrl { get; set; } = default!;
    public IFlurlClient Client => _client;

    public const string ApiEndpointHealthCheck = "/api/System/HealthCheck";

    public const string? SkipTest = "not require every time";

    public async Task InitializeAsync()
    {
#pragma warning disable CS8793 // The given expression always matches the provided pattern.
        if (SkipTest is not null) return;
#pragma warning restore CS8793 // The given expression always matches the provided pattern.

        var marsImage = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), string.Empty)
            .WithDockerfile("Dockerfile")
            .WithName("mars-docker-build:latest")
            .WithLogger(LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("Testcontainers"))// See it VS2022: Outupt -> Tests
            .Build();

        await marsImage.CreateAsync().ConfigureAwait(false);

        _network = new NetworkBuilder()
            .WithName(_networkName)
            .Build();

        await _network.CreateAsync();

        _postgresContainer = new PostgreSqlBuilder("postgres:14")
            .WithName("b-test-postgres")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithDatabase("test_db_source")
            .WithNetwork(_network)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready"))
            .WithCleanUp(true)
            .Build();

        await _postgresContainer.StartAsync().ConfigureAwait(false);

        var appConnectionString = $"Host=b-test-postgres:5432;Database=test_db_source;Username=postgres;Password=postgres";

        _marsContainer = new ContainerBuilder(marsImage)
            .WithName("b-test-mars")
            .WithPortBinding(80, assignRandomHostPort: true)
            .WithNetwork(_network)
            .DependsOn(_postgresContainer)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .AddCustomWaitStrategy(new WaitUntilHealthCheckOk()))
            .WithEnvironment("ASPNETCORE_URLS", "http://+:80")
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Production")
            .WithEnvironment("ConnectionStrings__DefaultConnection", appConnectionString)
            .WithEnvironment("Urls", "http://0.0.0.0:80")
            .WithCleanUp(true)
            .Build();

        await _marsContainer.StartAsync().ConfigureAwait(false);

        MarsUrl = "http://localhost" + ":" + _marsContainer.GetMappedPublicPort(80);

        _client = new FlurlClient(MarsUrl);
    }

    public async Task DisposeAsync()
    {
#pragma warning disable CS8793 // The given expression always matches the provided pattern.
        if (SkipTest is not null) return;
#pragma warning restore CS8793 // The given expression always matches the provided pattern.

        await _postgresContainer.DisposeAsync();
        await _marsContainer.DisposeAsync();
        await _network.DisposeAsync();
    }

    // Custom wait strategy implementation
    public class WaitUntilHealthCheckOk : IWaitUntil
    {
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        public async Task<bool> UntilAsync(IContainer container)
        {
            try
            {
                var url = $"http://{container.Hostname}:{container.GetMappedPublicPort(80)}{ApiEndpointHealthCheck}";
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                return content.Contains("OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Health check failed: {ex.Message}");
                return false;
            }
        }
    }

    public Task Reset()
    {
        return Task.CompletedTask;
    }
}
