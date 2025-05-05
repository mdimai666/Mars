using Mars.Core.Models;
using Microsoft.AspNetCore.Http.Features;

namespace Mars.Host.Shared.Models;

public class MarsAppFront
{
    public AppFrontSettingsCfg Configuration { get; set; } = default!;
    public FeatureCollection Features { get; set; } = new FeatureCollection();
}
