namespace Mars.UseStartup;

public static class ConfigureAppConfigurationExtensiions
{
    public static void ConfigureAppConfiguration(this WebApplicationBuilder builder, string[] args)
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

            builder.Configuration
                .AddJsonFile(
                    cfgpath,
                     optional: false,
                     reloadOnChange: false);
        }
        else if (env_cfg is not null)
        {
            builder.Configuration
                .AddJsonFile(
                    env_cfg,
                     optional: false,
                     reloadOnChange: false);
        }
        else
        {
            builder.Configuration
                .AddJsonFile(
                    "appsettings.Local.json",
                     optional: true,
                     reloadOnChange: false);
        }

    }
}
