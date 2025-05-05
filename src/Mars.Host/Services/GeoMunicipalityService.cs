using System.Linq.Expressions;
using AppShared.Models;
using Microsoft.Extensions.Configuration;

namespace Mars.Host.Services;

public class GeoMunicipalityService : StandartModelService<GeoMunicipality>
{
    public GeoMunicipalityService(IConfiguration configuration, IServiceProvider serviceProvider) : base(configuration, serviceProvider)
    {
    }

    public override Task<GeoMunicipality> Get(Guid id)
    {
        return base.Get(id, x => x.MunicType, x => x.Reg);
    }

    public override Task<TotalResponse<GeoMunicipality>> ListTable(QueryFilter filter, Expression<Func<GeoMunicipality, bool>> predicate = null)
    {
        return base.ListTable(filter, predicate, s => s.MunicType);
    }

    public override Task<GeoMunicipality> Update(Guid id, GeoMunicipality entity, Expression<Func<GeoMunicipality, object>>[]? include = null)
    {
        return base.Update(id, entity, include ?? [s => s.GeoMunicInfo]);
    }
}

