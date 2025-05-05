using AppShared.Models;
using Microsoft.Extensions.Configuration;

namespace Mars.Host.Services;

public class GeoRegionCenterService : StandartModelService<GeoRegionCenter>
{
    public GeoRegionCenterService(IConfiguration configuration, IServiceProvider serviceProvider) : base(configuration, serviceProvider)
    {
    }
}

