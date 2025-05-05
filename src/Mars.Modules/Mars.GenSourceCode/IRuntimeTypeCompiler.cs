using Mars.Host.Data.Entities;
using Mars.Host.Shared.Services;

namespace Mars.GenSourceCode;

public interface IRuntimeTypeCompiler
{
    public Dictionary<string, Type> Compile(List<PostTypeEntity> postTypeList, List<MetaFieldEntity> userMetaFields, IMetaModelTypesLocator mlocator, string setNamespace = "AppFront.Host.Data");
}
