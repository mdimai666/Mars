using Mars.Host.Data.Common;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.Data;

public static class MarsDbContextHelper
{
    public static IQueryable<IBasicEntity> DbSetByType(Type type, MarsDbContext ef, IServiceProvider sp)
    {
        IQueryable<IBasicEntity> q = null;

        switch (type.Name)
        {
            case nameof(PostEntity): q = ef.Set<PostEntity>().AsQueryable(); break;
            case nameof(UserEntity): q = ef.Set<UserEntity>().AsQueryable(); break;
            case nameof(NavMenuEntity): q = ef.Set<NavMenuEntity>().AsQueryable(); break;
            case nameof(FileEntity): q = ef.Set<FileEntity>().AsQueryable(); break;
            //case nameof(PostCategoryEntity): q = ef.Set<PostCategory>().AsQueryable(); break;
            case nameof(PostTypeEntity): q = ef.Set<PostTypeEntity>().AsQueryable(); break;
            case nameof(RoleEntity): q = ef.Set<RoleEntity>().AsQueryable(); break;
            case nameof(OptionEntity): q = ef.Set<OptionEntity>().AsQueryable(); break;

            //case nameof(GeoLocationEntity): q = ef.Set<GeoLocation>().AsQueryable(); break;
            //case nameof(GeoLocationTypeEntity): q = ef.Set<GeoLocationType>().AsQueryable(); break;
            //case nameof(GeoMunicipalityEntity): q = ef.Set<GeoMunicipality>().AsQueryable(); break;
            //case nameof(GeoMunicTypeEntity): q = ef.Set<GeoMunicType>().AsQueryable(); break;
            //case nameof(GeoRegionEntity): q = ef.Set<GeoRegion>().AsQueryable(); break;
            //case nameof(GeoRegionCenterEntity): q = ef.Set<GeoRegionCenter>().AsQueryable(); break;
        }

        IMetaModelTypesLocator mlocator = sp.GetRequiredService<IMetaModelTypesLocator>();

        //if (q is null) q = mlocator.GetModelQueryable(sp, type.Name);

        //if (q is not null) return q;

        throw new NotImplementedException();
    }
}
