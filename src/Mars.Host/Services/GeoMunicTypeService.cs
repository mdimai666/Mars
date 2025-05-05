using AppShared.Models;
using Microsoft.Extensions.Configuration;

namespace Mars.Host.Services;

public class GeoMunicTypeService : StandartModelService<GeoMunicType>
{
    public GeoMunicTypeService(IConfiguration configuration, IServiceProvider serviceProvider) : base(configuration, serviceProvider)
    {
    }
}

