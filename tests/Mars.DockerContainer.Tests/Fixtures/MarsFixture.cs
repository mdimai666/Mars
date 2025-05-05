using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Flurl.Http;
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
            .WithName("mars-docker-build")
            .Build();

        await marsImage.CreateAsync().ConfigureAwait(false);

        _network = new NetworkBuilder()
            .WithName(_networkName)
            .Build();

        await _network.CreateAsync();

        _postgresContainer = new PostgreSqlBuilder()
            .WithName("b-test-postgres")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithDatabase("test_db_source")
            .WithImage("postgres:14")
            .WithNetwork(_network)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready"))
            .WithCleanUp(true)
            .Build();

        await _postgresContainer.StartAsync().ConfigureAwait(false);

        var appConnectionString = $"Host=b-test-postgres:5432;Database=test_db_source;Username=postgres;Password=postgres";

        _marsContainer = new ContainerBuilder()
            .WithImage(marsImage)
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

        await _marsContainer.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    // Custom wait strategy implementation
    public class WaitUntilHealthCheckOk : IWaitUntil
    {
        public async Task<bool> UntilAsync(IContainer container)
        {
            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync($"http://{container.Hostname}:{container.GetMappedPublicPort(80)}{ApiEndpointHealthCheck}");
                var content = await response.Content.ReadAsStringAsync();
                return content.Contains("OK");
            }
            catch
            {
                return false;
            }
        }
    }

    public Task Reset()
    {
        return Task.CompletedTask;
    }
}
