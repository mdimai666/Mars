using AppShared.Models;
using Microsoft.Extensions.Configuration;

namespace Mars.Host.Services;

public class GeoLocationTypeService : StandartModelService<GeoLocationType>
{
    public GeoLocationTypeService(IConfiguration configuration, IServiceProvider serviceProvider) : base(configuration, serviceProvider)
    {
    }
}

