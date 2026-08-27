using Mars.Host.Shared.Dto.PostCategories;
using Mars.Shared.Common;
using Mars.Shared.Contracts.PostCategories;

namespace Mars.Host.Shared.Services;

public interface IPostCategoryService
{
    Task<PostCategorySummary?> Get(Guid id, CancellationToken cancellationToken);
    Task<PostCategoryDetail?> GetDetail(Guid id, CancellationToken cancellationToken);
    Task<PostCategoryDetail?> GetDetailBySlug(string slug, string type, CancellationToken cancellationToken);
    Task<ListDataResult<PostCategorySummary>> List(ListPostCategoryQuery query, CancellationToken cancellationToken);
    Task<PagingResult<PostCategorySummary>> ListTable(ListPostCategoryQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PostCategorySummary>> ListSummaryByIds(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    Task<PostCategoryDetail> Create(CreatePostCategoryQuery query, CancellationToken cancellationToken);
    Task<PostCategoryEditViewModel> GetEditModel(Guid id, CancellationToken cancellationToken);
    Task<PostCategoryEditViewModel> GetEditModelBlank(string categoryType, string postType, CancellationToken cancellationToken);
    PostCategoryEditDetail GetPostCategoryBlank(string categoryType, string postType);
    Task<PostCategoryDetail> Update(UpdatePostCategoryQuery query, CancellationToken cancellationToken);
    Task<PostCategorySummary> Delete(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PostCategorySummary>> DeleteMany(DeleteManyPostCategoryQuery query, CancellationToken cancellationToken);
    CreatePostCategoryQuery EnrichQuery(CreatePostCategoryRequest request);
    UpdatePostCategoryQuery EnrichQuery(UpdatePostCategoryRequest request);
}
