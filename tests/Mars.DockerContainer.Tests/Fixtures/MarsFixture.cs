using System.Diagnostics;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Flurl.Http;
using Testcontainers.PostgreSql;

namespace Mars.DockerContainer.Tests.Fixtures;

public class MarsFixture : IAsyncLifetime
{
    private const string MarsImageTag = "mars-docker-build:latest";
    private const string ApiEndpointHealthCheck = "/api/System/HealthCheck";

    private IContainer _marsContainer = default!;
    private IContainer _postgresContainer = default!;
    private INetwork _network = default!;
    private IFlurlClient _client = default!;

    /// <summary>
    /// Тесты с сборкой и запуском docker-контейнера тяжёлые, поэтому включаются явно:
    /// MARS_DOCKER_TESTS=1 dotnet test
    /// </summary>
    public static bool DockerTestsEnabled => Environment.GetEnvironmentVariable("MARS_DOCKER_TESTS")?.Trim() == "1";

    public string MarsUrl { get; private set; } = default!;
    public IFlurlClient Client => _client;

    public async Task InitializeAsync()
    {
        if (!DockerTestsEnabled) return;

        var solutionRoot = GetSolutionRoot();
        await BuildMarsImageAsync(solutionRoot);

        _network = new NetworkBuilder()
            .Build();

        await _network.CreateAsync();

        _postgresContainer = new PostgreSqlBuilder("postgres:15-alpine")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithDatabase("test_db_source")
            .WithNetwork(_network)
            .WithNetworkAliases("db")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready"))
            .WithCleanUp(true)
            .Build();

        await _postgresContainer.StartAsync().ConfigureAwait(false);

        _marsContainer = new ContainerBuilder(MarsImageTag)
            .WithPortBinding(80, assignRandomHostPort: true)
            .WithNetwork(_network)
            .DependsOn(_postgresContainer)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Production")
            .WithEnvironment("ASPNETCORE_URLS", "http://+:80")
            .WithEnvironment("ConnectionStrings__DefaultConnection",
                "Host=db;Database=test_db_source;Username=postgres;Password=postgres")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .AddCustomWaitStrategy(new WaitUntilHealthCheckOk(),
                    waitStrategy => waitStrategy.WithTimeout(TimeSpan.FromMinutes(5))))
            .WithCleanUp(true)
            .Build();

        await _marsContainer.StartAsync().ConfigureAwait(false);

        MarsUrl = "http://localhost:" + _marsContainer.GetMappedPublicPort(80);

        _client = new FlurlClient(MarsUrl);
    }

    public async Task DisposeAsync()
    {
        if (!DockerTestsEnabled) return;

        if (_client is not null)
            _client.Dispose();
        if (_marsContainer is not null)
            await _marsContainer.DisposeAsync();
        if (_postgresContainer is not null)
            await _postgresContainer.DisposeAsync();
        if (_network is not null)
            await _network.DisposeAsync();
    }

    // Dockerfile использует RUN --mount=type=cache (BuildKit), который недоступен
    // при сборке образа через API Testcontainers (legacy-эндпоинт /build),
    // поэтому образ собирается docker CLI (buildx/BuildKit) - как при обычной сборке.
    private static async Task BuildMarsImageAsync(string solutionRoot)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "docker",
            ArgumentList = { "build", "-t", MarsImageTag, "." },
            WorkingDirectory = solutionRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Не удалось запустить docker build");

        process.OutputDataReceived += (_, e) => { if (e.Data is not null) Console.WriteLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) Console.Error.WriteLine(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"docker build завершился с кодом {process.ExitCode}");
    }

    private static string GetSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !dir.EnumerateFiles("*.sln*").Any())
            dir = dir.Parent;

        return dir?.FullName
            ?? throw new DirectoryNotFoundException("Не найден файл решения (*.sln / *.slnx)");
    }

    // Custom wait strategy implementation
    private class WaitUntilHealthCheckOk : IWaitUntil
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
}
