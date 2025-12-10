using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.PostTypes;

namespace Mars.Host.Shared.Services;

public interface IMetaModelTypesLocator
{
    PostTypeDetail? GetPostTypeByName(string postTypeName);
    PostTypeDetail? GetPostTypeById(Guid id);
    IReadOnlyDictionary<string, PostTypeDetail> PostTypesDict();
    IReadOnlyCollection<string> ListMetaRelationModelProviderKeys();
    IMetaRelationModelProviderHandler? GetMetaRelationModelProvider(string modelName);
    IReadOnlyCollection<MetaRelationModel> AllMetaRelationsStructure();

    void InvalidateCompiledMetaMtoModels();
    Dictionary<string, Type> MetaMtoModelsCompiledTypeDict { get; }
    void UpdateMetaModelMtoRuntimeCompiledTypes();
    void TryUpdateMetaModelMtoRuntimeCompiledTypes();
    Task<string> MetaTypesSourceCode(string lang = "csharp");
}
