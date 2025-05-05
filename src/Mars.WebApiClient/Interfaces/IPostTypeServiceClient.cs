using Mars.Core.Exceptions;
using Mars.Shared.Common;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.PostTypes;

namespace Mars.WebApiClient.Interfaces;

public interface IPostTypeServiceClient
{
    Task<PostTypeDetailResponse?> Get(Guid id);

    /// <summary>
    /// Создает Тип записи
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="MarsValidationException"></exception>
    /// <exception cref="UserActionException"></exception>
    Task<PostTypeSummaryResponse> Create(CreatePostTypeRequest request);
    Task<PostTypeSummaryResponse> Update(UpdatePostTypeRequest request);
    Task<ListDataResult<PostTypeListItemResponse>> List(ListPostTypeQueryRequest filter);
    Task<PagingResult<PostTypeListItemResponse>> ListTable(TablePostTypeQueryRequest filter);
    Task Delete(Guid id);
    Task<PostTypeEditViewModel> GetEditModel(Guid id);

    Task<UserActionResult> PostTypeImport(string json);
    Task<UserActionResult> PostTypeImport(string json, string asPostType);

    Task<IReadOnlyCollection<MetaRelationModelResponse>> AllMetaRelationsStructure();
    Task<ListDataResult<MetaValueRelationModelSummaryResponse>> ListMetaValueRelationModels(MetaValueRelationModelsListQueryRequest request);
    Task<IReadOnlyDictionary<Guid, MetaValueRelationModelSummaryResponse>> GetMetaValueRelationModels(string modelName, Guid[] ids);

}
