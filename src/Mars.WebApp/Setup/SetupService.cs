using System.Text.Json;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.UseStartup.MarsParts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Mars.Setup;

public class SetupService
{
    // Intermediate data collected during wizard steps
    public string DbHost { get; set; } = "127.0.0.1";
    public int DbPort { get; set; } = 5432;
    public string DbName { get; set; } = "mars";
    public string DbUser { get; set; } = "mars";
    public string DbPassword { get; set; } = "mars";

    public string SiteUrl { get; set; } = "";
    public string SiteName { get; set; } = "Mars";
    public string SiteDescription { get; set; } = "";
    public string LoggingLevel { get; set; } = "Information";
    public string AppFrontMode { get; set; } = "HandlebarsTemplate";
    public string AppFrontStaticPath { get; set; } = "../client";

    public string AdminEmail { get; set; } = "admin@example.com";
    public string AdminPassword { get; set; } = "";
    public string AdminFirstName { get; set; } = "Admin";

    public async Task<(bool Success, string Message)> TestDatabaseConnectionAsync(
        string host, int port, string database, string username, string password)
    {
        var connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password}";

        try
        {
            using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT version();";
            var version = (await cmd.ExecuteScalarAsync())?.ToString() ?? "unknown";

            return (true, $"Подключение успешно! PostgreSQL версия: {version}");
        }
        catch (Exception ex)
        {
            return (false, $"Ошибка подключения: {ex.Message}");
        }
    }

    public void WriteLocalConfig()
    {
        var configPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Local.json");

        var config = new Dictionary<string, object>
        {
            ["ConnectionStrings"] = new Dictionary<string, string>
            {
                ["DefaultConnection"] = $"Host={DbHost};Port={DbPort};Database={DbName};Username={DbUser};Password={DbPassword}"
            },
            ["Setup"] = new Dictionary<string, string>
            {
                ["AdminEmail"] = AdminEmail,
                ["AdminPassword"] = AdminPassword,
                ["AdminFirstName"] = AdminFirstName,
                ["SiteUrl"] = SiteUrl,
                ["SiteName"] = SiteName,
                ["SiteDescription"] = SiteDescription
            },
            ["Logging"] = new Dictionary<string, object>
            {
                ["LogLevel"] = new Dictionary<string, string>
                {
                    ["Default"] = LoggingLevel
                }
            },
            ["AppFront"] = new[]
            {
                new Dictionary<string, string>
                {
                    ["Mode"] = AppFrontMode,
                    ["Path"] = AppFrontStaticPath,
                    ["Url"] = ""
                }
            }
        };

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(configPath, json);
    }

    public static async Task RunMigrationsAndSeedAsync(IServiceProvider serviceProvider, IConfiguration configuration, ILogger logger)
    {
        using var scope = serviceProvider.CreateScope();
        var marsDbContext = scope.ServiceProvider.GetRequiredService<MarsDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserEntity>>();

        // Run migrations
        var migrations = marsDbContext.Database.GetPendingMigrations();
        if (migrations.Any())
        {
            logger.LogWarning("[Setup] Running migrations: " + string.Join(", ", migrations));
            await marsDbContext.Database.MigrateAsync();
        }

        // Seed data
        MarsStartupPartMigrations.SeedData(serviceProvider, configuration, logger, true);
    }
}
