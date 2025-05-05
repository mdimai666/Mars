using System.Linq.Expressions;
using AppShared.Models;
using Microsoft.Extensions.Configuration;

namespace Mars.Host.Services;

public class GeoRegionService : StandartModelService<GeoRegion>
{
    public GeoRegionService(IConfiguration configuration, IServiceProvider serviceProvider) : base(configuration, serviceProvider)
    {
    }

    public override Task<GeoRegion> Update(Guid id, GeoRegion entity, Expression<Func<GeoRegion, object>>[]? include = null)
    {
        return base.Update(id, entity, include ?? [s => s.GeoRegionInfo]);
    }
}

