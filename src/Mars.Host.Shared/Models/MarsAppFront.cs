using Mars.Core.Models;
using Mars.Shared.Options;
using Microsoft.AspNetCore.Http.Features;

namespace Mars.Host.Shared.Models;

public class MarsAppFront
{
    public AppFrontSettingsCfg Configuration { get; set; } = default!;
    public FeatureCollection Features { get; set; } = new FeatureCollection();

    /// <summary>
    /// Настройка фронта из FrontsOption (источник истины)
    /// </summary>
    public FrontItem? Front { get; set; }
}
