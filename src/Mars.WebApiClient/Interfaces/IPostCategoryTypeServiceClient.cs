using Mars.Core.Exceptions;
using Mars.Shared.Common;
using Mars.Shared.Contracts.PostCategoryTypes;

namespace Mars.WebApiClient.Interfaces;

public interface IPostCategoryTypeServiceClient
{
    Task<PostCategoryTypeDetailResponse?> Get(Guid id);
    Task<PostCategoryTypeDetailResponse?> GetByName(string typeName);

    /// <summary>
    /// Создает Тип категории
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="MarsValidationException"></exception>
    /// <exception cref="PostCategoryActionException"></exception>
    Task<PostCategoryTypeSummaryResponse> Create(CreatePostCategoryTypeRequest request);
    Task<PostCategoryTypeSummaryResponse> Update(UpdatePostCategoryTypeRequest request);
    Task<ListDataResult<PostCategoryTypeListItemResponse>> List(ListPostCategoryTypeQueryRequest filter);
    Task<PagingResult<PostCategoryTypeListItemResponse>> ListTable(TablePostCategoryTypeQueryRequest filter);
    Task Delete(Guid id);
    Task DeleteMany(Guid[] ids);
    Task<PostCategoryTypeEditViewModel> GetEditModel(Guid id);

}
