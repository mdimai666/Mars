using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.PostTypes;

namespace Mars.Host.Shared.Services;

public interface IMetaModelTypesLocator
{
    PostTypeDetail? GetPostTypeByName(string postTypeName);
    IReadOnlyDictionary<string, PostTypeDetail> PostTypesDict();
    IReadOnlyCollection<string> ListMetaRelationModelProviderKeys();
    IMetaRelationModelProviderHandler? GetMetaRelationModelProvider(string modelName);
    IReadOnlyCollection<MetaRelationModel> AllMetaRelationsStructure();

    void InvalidateCompiledMetaMtoModels();
    Dictionary<string, Type> MetaMtoModelsCompiledTypeDict { get; }
    void UpdateMetaModelMtoRuntimeCompiledTypes(IServiceProvider serviceProvider);
    void TryUpdateMetaModelMtoRuntimeCompiledTypes(IServiceProvider serviceProvider);

}
