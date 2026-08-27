using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Shared.Common;
using Mars.Shared.Contracts.PostTypes;

namespace Mars.Host.Shared.Services;

public interface IPostTypeService
{
    Task<PostTypeSummary?> Get(Guid id, CancellationToken cancellationToken);
    Task<PostTypeDetail?> GetDetail(Guid id, CancellationToken cancellationToken);
    Task<ListDataResult<PostTypeSummary>> List(ListPostTypeQuery query, CancellationToken cancellationToken);
    Task<PagingResult<PostTypeSummary>> ListTable(ListPostTypeQuery query, CancellationToken cancellationToken);
    Task<PostTypeDetail> Create(CreatePostTypeQuery query, CancellationToken cancellationToken);
    Task<PostTypeEditViewModel> GetEditModel(Guid id, CancellationToken cancellationToken);
    Task<PostTypeDetail> Update(UpdatePostTypeQuery query, CancellationToken cancellationToken);
    Task<PostTypeSummary> Delete(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PostTypeSummary>> DeleteMany(DeleteManyPostTypeQuery query, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<MetaRelationModel>> AllMetaRelationsStructure();
    Task<ListDataResult<MetaValueRelationModelSummary>> ListMetaValueRelationModels(MetaValueRelationModelsListQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, MetaValueRelationModelSummary>> GetMetaValueRelationModels(string modelName, Guid[] ids, CancellationToken cancellationToken);
    Task UpdatePresentation(UpdatePostTypePresentationQuery query, CancellationToken cancellationToken);
    PostTypePresentationEditViewModel? GetPresentationEditModel(Guid id, CancellationToken cancellationToken);
}
