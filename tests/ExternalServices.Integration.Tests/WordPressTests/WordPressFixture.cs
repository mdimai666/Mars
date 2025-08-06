using System.Net.NetworkInformation;
using Bogus;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Flurl.Http;
using MySqlConnector;
using Testcontainers.MySql;

namespace ExternalServices.Integration.Tests.WordPressTests;

public class WordPressFixture : IAsyncLifetime
{
    private MySqlContainer _mysqlContainer = default!;
    private IContainer _wordPressContainer = default!;
    private readonly Faker<WpPost> _postFaker;
    private IFlurlClient _client = default!;
    private INetwork _network = default!;
    private const string _networkName = "mars-test-network";

    public IFlurlClient Client => _client;

    public string ConnectionString => _mysqlContainer.GetConnectionString();
    public string WordPressUrl { get; private set; } = "";
    //public readonly string HostName = "mars-wordpress";

    public WordPressFixture()
    {
        // Initialize Bogus to generate fake posts
        _postFaker = new Faker<WpPost>()
            .RuleFor(p => p.Title, f => f.Lorem.Sentence())
            .RuleFor(p => p.Content, f => f.Lorem.Paragraphs(3))
            .RuleFor(p => p.Status, "publish");
    }

    public async Task InitializeAsync()
    {
        await EnsureWpCliInTempPath();

        _network = new NetworkBuilder()
            .WithName(_networkName)
            .Build();

        await _network.CreateAsync();

        _mysqlContainer = new MySqlBuilder()
            .WithName("b-test-mysql")
            .WithImage("mysql:8.0")
            .WithDatabase("wordpress")
            .WithUsername("wordpress")
            .WithPassword("wordpress")
            .WithNetwork(_network)
            .WithHostname("db")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("mysqladmin", "ping", "-h", "localhost"))
            .WithCleanUp(true)
            .Build();

        await _mysqlContainer.StartAsync();

        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        await CreateDatabase(connection);

        var db_host = "b-test-mysql:3306";
        var wp_port = GetNextFreePort(10100);
        var wp_url = $"http://localhost:{wp_port}";

        var wpEnv = new Dictionary<string, string>()
        {
            ["WORDPRESS_DB_HOST"] = db_host,
            ["WORDPRESS_DB_USER"] = "wordpress",
            ["WORDPRESS_DB_PASSWORD"] = "wordpress",
            ["WORDPRESS_DB_NAME"] = "wordpress",
            ["WORDPRESS_TABLE_PREFIX"] = "wp_",
            ["WORDPRESS_DEBUG"] = "1",

            ["WP_URL"] = wp_url,
        };

        var wp_myAuth = "wp-my-auth.php";
        var wp_install = @"wp-install.sh";
        var wp_dest_dir = "/var/www/html/";

        _wordPressContainer = new ContainerBuilder()
            .WithImage("wordpress:latest")
            .WithName("b-test-wordpress")
            .WithNetwork(_network)
            .WithHostname("wp")
            .WithPortBinding(wp_port, 80)
            .WithEnvironment(wpEnv)
            .DependsOn(_mysqlContainer)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(80))
            .WithCleanUp(true)
            .WithBindMount(Path.Combine(MountFilesDir, wp_install), wp_dest_dir + wp_install)
            .WithBindMount(Path.Combine(MountFilesDir, wp_myAuth), wp_dest_dir + wp_myAuth)
            .WithBindMount(WpCliPath(), "/usr/local/bin/wp")
            .Build();

        await _wordPressContainer.StartAsync();

        var wordpressUrl = "http://localhost" + ":" + _wordPressContainer.GetMappedPublicPort(80);
        WordPressUrl = wordpressUrl;

        // run initial setup
        var installScript = await _wordPressContainer.ExecAsync(["bash", "/var/www/html/wp-install.sh"]);
        if (installScript.ExitCode != 0) throw new InvalidOperationException("INSTALL SCRIPT ERROR: " + installScript.Stdout + "\n\n" + installScript.Stderr);

        _client = new FlurlClient(wordpressUrl);
        await AuthenticateWithWordPress();
        //await CreateMockPosts(10);
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        await _wordPressContainer.StopAsync();
        await _mysqlContainer.StopAsync();
    }

    public Task Reset()
    {
        //await using var connection = new MySqlConnection(_container.GetConnectionString());
        //await connection.OpenAsync();
        //await RemoveAllTablesAsync(connection);

        //await _respawner.ResetAsync(connection);
        return Task.CompletedTask;
    }

    private async Task CreateMockPosts(int numberOfPosts)
    {
        for (int i = 0; i < numberOfPosts; i++)
        {
            var post = _postFaker.Generate();
            var response = await _client.Request("/wp-json/wp/v2/posts").PostJsonAsync(post).ReceiveString();

            Console.WriteLine($"Created post {i + 1} with title: {post.Title}");
        }
    }

    private async Task CreateDatabase(MySqlConnection connection, string databaseName = "wordpress")
    {
        var createDbCommand = new MySqlCommand($"CREATE DATABASE IF NOT EXISTS {databaseName};", connection);
        await createDbCommand.ExecuteNonQueryAsync();

        Console.WriteLine($"Database '{databaseName}' created or already exists.");
    }

    private async Task AuthenticateWithWordPress()
    {
        // If your WordPress instance requires authentication, you can authenticate here
        // Example: Use basic auth or JWT token
        // For simplicity, this example assumes no authentication is required

        // not work! cookie not writted
        var req = await _client.Request("/wp-my-auth.php").WithAutoRedirect(true).PostAsync();
        var status = req.StatusCode;
        var cookies = req.Cookies;
        var body = await req.GetStringAsync();
    }

    protected string MountFilesDir => Path.Combine(Path.GetFullPath("../../..", Environment.CurrentDirectory), "WordPressTests", "MountFiles");

    protected virtual int GetNextFreePort(int port = 0)
    {
        port = port > 0 ? port : new Random().Next(1, 65535);
        while (!IsPortFree(port))
        {
            port += 1;
        }
        return port;
    }

    protected virtual bool IsPortFree(int port)
    {
        var properties = IPGlobalProperties.GetIPGlobalProperties();
        var listeners = properties.GetActiveTcpListeners();
        var openPorts = listeners.Select(item => item.Port).ToArray();

        return openPorts.All(openPort => openPort != port);
    }

    private const string WpCliTempDir = "t-Mars-wp-cli";
    private const string wpCliFileName = "wp-cli.phar";

    private string WpCliPath() => Path.Combine(Path.GetTempPath(), WpCliTempDir, wpCliFileName);

    private async Task EnsureWpCliInTempPath()
    {
        var wpCliUrl = @"https://raw.githubusercontent.com/wp-cli/builds/gh-pages/phar/wp-cli.phar";

        var wpCliFilePath = WpCliPath();

        if (!File.Exists(wpCliFilePath))
        {
            var dir = Path.GetDirectoryName(wpCliFilePath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var http = new HttpClient();
            var response = await http.GetAsync(wpCliUrl);
            using (var fs = new FileStream(wpCliFilePath, FileMode.CreateNew))
            {
                await response.Content.CopyToAsync(fs);
            }
        }
    }
}

// Define a Post class to match the WordPress REST API schema
public class WpPost
{
    //public int Id { get; set; }
    public string Title { get; set; } = default!;
    public string Content { get; set; } = default!;
    public string Status { get; set; } = default!;
}
