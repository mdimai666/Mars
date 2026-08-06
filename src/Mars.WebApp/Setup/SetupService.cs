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

    public void WriteLocalConfig(
        string host, int port, string database, string username, string password,
        string adminEmail, string adminPassword, string adminFirstName)
    {
        var configPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Local.json");

        var config = new Dictionary<string, object>
        {
            ["ConnectionStrings"] = new Dictionary<string, string>
            {
                ["DefaultConnection"] = $"Host={host};Port={port};Database={database};Username={username};Password={password}"
            },
            ["Setup"] = new Dictionary<string, string>
            {
                ["AdminEmail"] = adminEmail,
                ["AdminPassword"] = adminPassword,
                ["AdminFirstName"] = adminFirstName
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
