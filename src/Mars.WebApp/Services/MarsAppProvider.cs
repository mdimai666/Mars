using Mars.Core.Models;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Services;
using Mars.UseStartup;

namespace Mars.Services;

public class MarsAppProvider : IMarsAppProvider
{
    public Dictionary<string, MarsAppFront> Apps { get; set; } = new();

    public MarsAppFront FirstApp { get; set; }
    public bool SetupMultiApps { get; set; }

    public MarsAppProvider(ConfigurationManager configuration)
    {
        var _af = configuration.GetRequiredSection("AppFront").Get<AppFrontSettingsCfg>()!;
        var _afs = configuration.GetRequiredSection("AppFront").Get<AppFrontSettingsCfg[]>()!;

        var section = configuration.GetRequiredSection("AppFront");
        var sectionString = section.GetValue<string>("Mode");

        bool isSingleFront = sectionString is not null;

        SetupMultiApps = !isSingleFront && _afs.Count() > 0;

        if (!isSingleFront && !SetupMultiApps)
        {
            throw new ArgumentException("setup AppFront in 'appsettings.json'");
        }

        List<AppFrontSettingsCfg> appFrontsConfigs = new();

        if (SetupMultiApps)
        {
            foreach (var app in _afs.Where(s => s is not null))
            {
                appFrontsConfigs.Add(app);
            }
        }
        else
        {
            appFrontsConfigs.Add(_af);
            if (string.IsNullOrEmpty(_af.Url))
            {
                _af.Url = "";
            }
        }

        foreach (var appCfg in appFrontsConfigs)
        {
            MarsAppFront app = new MarsAppFront
            {
                Configuration = appCfg,
            };
            Apps.Add(appCfg.Url, app);
        }
        FirstApp = Apps.Values.First();
    }

    public MarsAppFront GetAppForUrl(string url)
    {
        url = url.ToLower();
        if (!SetupMultiApps)
        {
            return StartupFront.FirstAppFront;
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
