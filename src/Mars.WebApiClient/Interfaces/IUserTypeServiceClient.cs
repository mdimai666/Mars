using Mars.Core.Exceptions;
using Mars.Shared.Common;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.UserTypes;

namespace Mars.WebApiClient.Interfaces;

public interface IUserTypeServiceClient
{
    Task<UserTypeDetailResponse?> Get(Guid id);

    /// <summary>
    /// Создает Тип записи
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="MarsValidationException"></exception>
    /// <exception cref="UserActionException"></exception>
    Task<UserTypeSummaryResponse> Create(CreateUserTypeRequest request);
    Task<UserTypeSummaryResponse> Update(UpdateUserTypeRequest request);
    Task<ListDataResult<UserTypeListItemResponse>> List(ListUserTypeQueryRequest filter);
    Task<PagingResult<UserTypeListItemResponse>> ListTable(TableUserTypeQueryRequest filter);
    Task Delete(Guid id);
    Task DeleteMany(Guid[] ids);
    Task<UserTypeEditViewModel> GetEditModel(Guid id);

    Task<IReadOnlyCollection<MetaRelationModelResponse>> AllMetaRelationsStructure();
    Task<ListDataResult<MetaValueRelationModelSummaryResponse>> ListMetaValueRelationModels(MetaValueRelationModelsListQueryRequest request);
    Task<IReadOnlyDictionary<Guid, MetaValueRelationModelSummaryResponse>> GetMetaValueRelationModels(string modelName, Guid[] ids);
}
