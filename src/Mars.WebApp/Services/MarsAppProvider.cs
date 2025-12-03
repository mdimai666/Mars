using Mars.Core.Models;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Services;

namespace Mars.Services;

public class MarsAppProvider : IMarsAppProvider
{
    public Dictionary<string, MarsAppFront> Apps { get; set; } = new();

    public MarsAppFront FirstApp { get; set; }
    public bool SetupMultiApps { get; set; }

    public MarsAppProvider(ConfigurationManager configuration)
    {
        var appFrontSettings = ReadConfig(configuration);

        if (appFrontSettings is null) appFrontSettings = [new()];

        SetupMultiApps = appFrontSettings.Count(s => s.Mode != AppFrontMode.None) > 1;

        foreach (var appCfg in appFrontSettings.Where(s => s is not null))
        {
            MarsAppFront app = new MarsAppFront
            {
                Configuration = appCfg,
            };
            Apps.Add(appCfg.Url, app);
        }
        FirstApp = Apps.Values.First();
    }

    private AppFrontSettingsCfg[] ReadConfig(ConfigurationManager configuration)
    {
        var section = configuration.GetRequiredSection("AppFront");
        var rootElementHasModeField = section.GetValue<string?>("Mode") is not null;

        return rootElementHasModeField ? [section.Get<AppFrontSettingsCfg>()!] : section.Get<AppFrontSettingsCfg[]>()!;
    }

    public MarsAppFront GetAppForUrl(string url)
    {
        url = url.ToLower();
        if (!SetupMultiApps)
        {
            return FirstApp;
        }
        else
        {
            if (url == "/") return Apps[""];
            foreach (var app in Apps.Values)
            {
                if (app.Configuration.Url != "" && url.StartsWith(app.Configuration.Url))
                {
                    return app;
                }
            }
        }
        return Apps[""];
    }
}
