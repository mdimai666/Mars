using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;

namespace Mars.UseStartup;

public static class ConfigureAppConfigurationExtensiions
{
    /// <summary>
    /// Docker: подключает конфиг, записанный setup-визардом (./config/appsettings.Production.json на томе).
    /// Вставляется перед env-источниками: приоритет выше дефолтов appsettings.json из образа,
    /// но env-переменные и явно примонтированный appsettings.Production.json перекрывают его.
    /// </summary>
    public static IConfigurationBuilder AddWizardConfigSource(this IConfigurationBuilder builder)
    {
        var root = Directory.GetCurrentDirectory();
        var wizardConfigPath = Path.Combine(root, "config", "appsettings.Production.json");
        if (!File.Exists(wizardConfigPath))
            return builder;

        var source = new JsonConfigurationSource
        {
            Path = Path.Combine("config", "appsettings.Production.json"),
            Optional = true,
            ReloadOnChange = true,
            FileProvider = new PhysicalFileProvider(root),
        };

        // Вставляем перед последним env-источником (маппит обычные переменные вида
        // ConnectionStrings__DefaultConnection): конфиг визарда перекрывает json-дефолты
        // образа, но env-переменные остаются приоритетнее.
        var insertAt = builder.Sources.Count;
        for (var i = builder.Sources.Count - 1; i >= 0; i--)
        {
            if (builder.Sources[i] is EnvironmentVariablesConfigurationSource)
            {
                insertAt = i;
                break;
            }
        }

        builder.Sources.Insert(insertAt, source);
        return builder;
    }

    public static IConfigurationBuilder ConfigureAppConfiguration(this IConfigurationBuilder builder, string[] args)
    {
        string? env_cfg = Environment.GetEnvironmentVariable("MARS_CFG");

        if (args.Contains("-cfg"))
        {
            int argsCfgIndex = args.ToList().IndexOf("-cfg");
            string cfgpath = args[argsCfgIndex + 1];

            if (!Path.IsPathRooted(cfgpath))
            {
                cfgpath = Path.Join(MarsStartupInfo.StartWorkDirectory, cfgpath);
            }

            builder.AddJsonFile(
                    cfgpath,
                     optional: false,
                     reloadOnChange: true);
        }
        else if (env_cfg is not null)
        {
            builder.AddJsonFile(
                    env_cfg,
                     optional: false,
                     reloadOnChange: true);
        }
        else
        {
            builder.AddJsonFile(
                    "appsettings.Local.json",
                     optional: true,
                     reloadOnChange: true);
        }

        return builder;
    }
}
