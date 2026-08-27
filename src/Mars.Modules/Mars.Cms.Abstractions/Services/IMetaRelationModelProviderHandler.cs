using Mars.Host.Shared.Dto.MetaFields;
using Mars.Shared.Common;

namespace Mars.Host.Shared.Services;

public interface IMetaRelationModelProviderHandler
{
    Task<Dictionary<Guid, object>> ListHandle(IReadOnlyCollection<Guid> ids, string modelName, CancellationToken cancellationToken);
    MetaRelationModel Structure();
    Task<ListDataResult<MetaValueRelationModelSummary>> ListData(MetaValueRelationModelsListQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, MetaValueRelationModelSummary>> GetIds(string modelName, Guid[] ids, CancellationToken cancellationToken);
    Task<int> DeleteMany(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}

public interface IMetaRelationModelProviderWithSubItemsHandler
{
    Task<RelationModelSubType[]> ListSubTypes();//переделать

}
