using AppShared.Models;
using Mars.Host.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Mars.Host.Services;

public class GeoLocationService : StandartModelService<GeoLocation>
{
    public GeoLocationService(IConfiguration configuration, IServiceProvider serviceProvider) : base(configuration, serviceProvider)
    {
    }

    public override Task<GeoLocation> Get(Guid id)
    {
        return base.Get(id, s => s.GeoLocationType, s => s.GeoMunicipality);
    }

    public override Task<TotalResponse<GeoLocation>> ListTable(QueryFilter filter, Expression<Func<GeoLocation, bool>> predicate = null)
    {
        return base.ListTable(filter, predicate, s => s.GeoLocationType);
    }

    public async Task<TotalResponse<GeoLocation>> SearchLocation(Guid regionId, QueryFilter queryFilter)
    {
        var ef = GetEFContext();

        var query = ef.GeoLocations
                .AsNoTracking()
                .Include(s => s.GeoMunicipality)
                .ThenInclude(s => s.Reg)
                .Where(s => s.GeoMunicipality.Reg.Id == regionId);

        if (string.IsNullOrEmpty(queryFilter.Search))
        {
            return await query.QueryTable(queryFilter);
        }
        else
        {
            //var searchText = queryFilter.Search.ToLower();
            //return await query.QueryTable(queryFilter, geo => geo.ShortName.ToLower().Contains(searchText));
            return await query.QueryTable(queryFilter, geo => EF.Functions.ILike(geo.ShortName, $"%{queryFilter.Search}%"));
        }
    }

}

