using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Shared.Models;

namespace Mars.Host.Shared.Services;

public interface IMetaModelTypesLocator
{
    PostTypeDetail? GetPostTypeByName(string postTypeName);
    PostTypeDetail? GetPostTypeById(Guid id);
    bool ExistPostType(Guid id);
    bool ExistPostType(string postTypeName);
    IReadOnlyDictionary<string, PostTypeDetail> PostTypesDict();
    IReadOnlyCollection<string> ListMetaRelationModelProviderKeys();
    IMetaRelationModelProviderHandler? GetMetaRelationModelProvider(string modelName);
    IReadOnlyCollection<MetaRelationModel> AllMetaRelationsStructure();

    void InvalidateCompiledMetaMtoModels();
    IReadOnlyDictionary<string, MtoModelInfo> MetaMtoModelsCompiledTypeDict { get; }
    void UpdateMetaModelMtoRuntimeCompiledTypes();
    void TryUpdateMetaModelMtoRuntimeCompiledTypes();
    Task<string> MetaTypesSourceCode(string lang = "csharp");
    MetaModelSourceResult? ResolveEntityNameToSourceUri(string entityName);
}

public record MetaModelSourceResult
{
    public required SourceUri EntityUri { get; init; }
    public required Type MetaEntityModelType { get; init; }
    public required Type BaseEntityType { get; init; }
    public required bool IsMetaType { get; init; }
}
