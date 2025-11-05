namespace Mars.UseStartup;

public static class ConfigureAppConfigurationExtensiions
{
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
                     reloadOnChange: false);
        }
        else if (env_cfg is not null)
        {
            builder.AddJsonFile(
                    env_cfg,
                     optional: false,
                     reloadOnChange: false);
        }
        else
        {
            builder.AddJsonFile(
                    "appsettings.Local.json",
                     optional: true,
                     reloadOnChange: false);
        }

        return builder;
    }
}
