using Mars.Host.Shared.Dto.PostCategories;
using Mars.Shared.Common;

namespace Mars.Host.Shared.Repositories;

public interface IPostCategoryRepository : IDisposable
{
    Task<PostCategorySummary?> Get(Guid id, CancellationToken cancellationToken);
    Task<PostCategoryDetail?> GetDetail(Guid id, CancellationToken cancellationToken);
    Task<PostCategoryDetail?> GetDetailBySlug(string slug, string type, CancellationToken cancellationToken);
    Task<PostCategoryEditDetail?> GetPostCategoryEditDetail(Guid id, CancellationToken cancellationToken);

    Task<Guid> Create(CreatePostCategoryQuery query, CancellationToken cancellationToken);
    Task Update(UpdatePostCategoryQuery query, CancellationToken cancellationToken);
    Task Delete(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PostCategorySummary>> ListAll(ListAllPostCategoryQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PostCategoryDetail>> ListAllDetail(ListAllPostCategoryQuery query, CancellationToken cancellationToken);
    Task<ListDataResult<PostCategorySummary>> List(ListPostCategoryQuery query, CancellationToken cancellationToken);
    Task<PagingResult<PostCategorySummary>> ListTable(ListPostCategoryQuery query, CancellationToken cancellationToken);
    Task<ListDataResult<PostCategoryDetail>> ListDetail(ListPostCategoryQuery query, CancellationToken cancellationToken);
    Task<PagingResult<PostCategoryDetail>> ListTableDetail(ListPostCategoryQuery query, CancellationToken cancellationToken);
    Task<bool> ExistAsync(Guid id, CancellationToken cancellationToken);
    Task<int> DeleteMany(DeleteManyPostCategoryQuery query, CancellationToken cancellationToken);
    Task RecalculateCategoryPathHierarchyFallback(Guid postCategoryTypeId, Guid updatingItemId, bool force, CancellationToken cancellationToken);
    Task RecalculateCategoryTypePathHierarchy(Guid postCategoryTypeId, bool force, CancellationToken cancellationToken);
    Task RecalculateAllCategoryPathsHierarchy(CancellationToken cancellationToken);
    Task<bool> ExistAllAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}
