using Mars.Core.Exceptions;
using Mars.Shared.Common;
using Mars.Shared.Contracts.PostCategories;

namespace Mars.WebApiClient.Interfaces;

public interface IPostCategoryServiceClient
{
    Task<PostCategoryDetailResponse?> Get(Guid id);
    Task<PostCategoryDetailResponse?> GetBySlug(string slug, string type);

    /// <summary>
    /// Создает
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="MarsValidationException"></exception>
    /// <exception cref="UserActionException"></exception>
    Task<PostCategoryDetailResponse> Create(CreatePostCategoryRequest request);
    Task<PostCategoryDetailResponse> Update(UpdatePostCategoryRequest request);
    Task<ListDataResult<PostCategoryListItemResponse>> List(ListPostCategoryQueryRequest filter);
    Task<PagingResult<PostCategoryListItemResponse>> ListTable(TablePostCategoryQueryRequest filter);
    Task<ListDataResult<PostCategoryListItemResponse>> List(string categoryType, ListPostCategoryQueryRequest filter);
    Task<PagingResult<PostCategoryListItemResponse>> ListTable(string categoryType, TablePostCategoryQueryRequest filter);
    Task<ListDataResult<PostCategoryListItemResponse>> ListForPostType(string postType, ListPostCategoryQueryRequest filter);
    Task<PagingResult<PostCategoryListItemResponse>> ListTableForPostType(string postType, TablePostCategoryQueryRequest filter);
    Task Delete(Guid id);
    Task DeleteMany(Guid[] ids);

    Task<PostCategoryEditViewModel> GetEditModel(Guid id);
    Task<PostCategoryEditViewModel> GetBlankModel(string categoryType, string postType);
}
