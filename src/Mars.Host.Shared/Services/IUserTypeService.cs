using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.UserTypes;
using Mars.Shared.Common;
using Mars.Shared.Contracts.UserTypes;

namespace Mars.Host.Shared.Services;

public interface IUserTypeService
{
    Task<UserTypeSummary?> Get(Guid id, CancellationToken cancellationToken);
    Task<UserTypeDetail?> GetDetail(Guid id, CancellationToken cancellationToken);
    Task<ListDataResult<UserTypeSummary>> List(ListUserTypeQuery query, CancellationToken cancellationToken);
    Task<PagingResult<UserTypeSummary>> ListTable(ListUserTypeQuery query, CancellationToken cancellationToken);
    Task<UserTypeDetail> Create(CreateUserTypeQuery query, CancellationToken cancellationToken);
    Task<UserTypeEditViewModel> GetEditModel(Guid id, CancellationToken cancellationToken);
    Task<UserTypeDetail> Update(UpdateUserTypeQuery query, CancellationToken cancellationToken);
    Task<UserTypeSummary> Delete(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<UserTypeSummary>> DeleteMany(DeleteManyUserTypeQuery query, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<MetaRelationModel>> AllMetaRelationsStructure();
    Task<ListDataResult<MetaValueRelationModelSummary>> ListMetaValueRelationModels(MetaValueRelationModelsListQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, MetaValueRelationModelSummary>> GetMetaValueRelationModels(string modelName, Guid[] ids, CancellationToken cancellationToken);
}
