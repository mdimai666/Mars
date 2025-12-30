using Mars.Core.Exceptions;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Posts;

namespace Mars.WebApiClient.Interfaces;

public interface IPostServiceClient
{
    Task<PostDetailResponse?> Get(Guid id, bool renderContent = true);
    Task<PostDetailResponse?> GetBySlug(string slug, string type, bool renderContent = true);

    /// <summary>
    /// Создает
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="MarsValidationException"></exception>
    /// <exception cref="UserActionException"></exception>
    Task<PostDetailResponse> Create(CreatePostRequest request);
    Task<PostDetailResponse> Update(UpdatePostRequest request);
    Task<ListDataResult<PostListItemResponse>> List(ListPostQueryRequest filter);
    Task<PagingResult<PostListItemResponse>> ListTable(TablePostQueryRequest filter);
    Task<ListDataResult<PostListItemResponse>> List(string postType, ListPostQueryRequest filter);
    Task<PagingResult<PostListItemResponse>> ListTable(string postType, TablePostQueryRequest filter);
    Task Delete(Guid id);
    Task DeleteMany(Guid[] ids);

    Task<PostEditViewModel> GetEditModel(Guid id);
    Task<PostEditViewModel> GetPostBlank(string type);
}
