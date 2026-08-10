using System.Text.Json;
using Mars.Services;
using Mars.Shared.Options;
using Npgsql;

namespace Mars.Setup;

public class SetupService
{
    /// <summary>
    /// Значение выбора фронта в визарде: существующая папка с шаблонами (путь + движок).
    /// </summary>
    public const string ExistingFrontChoice = "existing";

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

    /// <summary>
    /// Выбор фронта: имя стартового шаблона (default/landing/...) или <see cref="ExistingFrontChoice"/>.
    /// </summary>
    public string FrontChoice { get; set; } = FrontTemplateService.DefaultTemplateName;
    public string FrontPath { get; set; } = "";
    public string FrontEngineId { get; set; } = FrontItem.HandlebarsEngine;

    /// <summary>
    /// Доступные стартовые шаблоны фронтов (папки в Res/front_templates, кроме служебного admin).
    /// </summary>
    public IReadOnlyList<string> GetAvailableFrontTemplates()
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "Res", "front_templates");
        if (!Directory.Exists(root)) return [];

        return Directory.GetDirectories(root)
            .Select(dir => Path.GetFileName(dir)!)
            .Where(name => !string.Equals(name, FrontTemplateService.AdminTemplateName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name)
            .ToList();
    }

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
        var configPath = SetupWizardHost.WizardConfigPath;
        var fullConfigPath = Path.GetFullPath(configPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullConfigPath)!);

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
                ["SiteDescription"] = SiteDescription,
                // выбор фронта в визарде — подбирает AppFrontMigration.EnsureDefaultFront при первом старте
                ["FrontChoice"] = FrontChoice,
                ["FrontPath"] = FrontPath,
                ["FrontEngineId"] = FrontEngineId
            },
            ["Logging"] = new Dictionary<string, object>
            {
                ["LogLevel"] = new Dictionary<string, string>
                {
                    ["Default"] = LoggingLevel
                }
            }
        };

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(fullConfigPath, json);
    }
}
